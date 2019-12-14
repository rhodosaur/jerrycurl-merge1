﻿using Jerrycurl.Data;
using Jerrycurl.Data.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jerrycurl.Data
{
    public class AdoConnection : IDisposable
#if NETSTANDARD2_1
        , IAsyncDisposable
#endif
    {
        private readonly Func<IDbConnection> connectionFactory;

        private IDbConnection connection;
        private DbConnection connectionAsync;
        private bool wasDisposed = false;

        private readonly IFilterHandler[] filters;

        public AdoConnection(AdoOptions options)
        {
            this.connectionFactory = options?.ConnectionFactory ?? throw new AdoException("Connection factory is not initialized.");
            this.filters = options.Filters?.Select(f => f.GetHandler()).OfType<IFilterHandler>().ToArray() ?? Array.Empty<IFilterHandler>();
        }

        public IEnumerable<IDataReader> Execute(IAdoCommandBuilder builder)
        {
            IDbConnection connection = null;

            try
            {
                connection = this.GetOpenConnection();
            }
            catch (Exception ex)
            {
                this.ApplyConnectionFilters(h => h.OnConnectionException, ex);

                throw new AdoException("An error occurred opening the ADO.NET connection.", ex);
            }

            using IDbCommand dbCommand = connection.CreateCommand();

            try
            {
                builder.Build(dbCommand);
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred building the ADO.NET command object.", ex);
            }

            this.ApplyCommandFilters(h => h.OnCommandCreated, dbCommand);

            IDataReader reader = null;

            try
            {
                reader = dbCommand.ExecuteReader();
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred executing the ADO.NET command.", ex);
            }

            try
            {
                do yield return reader;
                while (reader.NextResult());
            }
            finally
            {
                reader?.Dispose();
            }

            this.ApplyCommandFilters(h => h.OnCommandExecuted, dbCommand);

        }

#if NETSTANDARD2_1
        public async IAsyncEnumerable<DbDataReader> ExecuteAsync(IAdoCommandBuilder builder, [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            DbConnection connection = await this.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using DbCommand dbCommand = connection.CreateCommand();

            try
            {
                builder.Build(dbCommand);
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred building the ADO.NET command object.", ex);
            }

            this.ApplyCommandFilters(h => h.OnCommandCreated, dbCommand);

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            DbDataReader reader = null;

            try
            {
                reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred executing the ADO.NET command.", ex);
            }

            try
            {
                do yield return reader;
                while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                reader?.Dispose();
            }

            this.ApplyCommandFilters(h => h.OnCommandExecuted, dbCommand);
        }
#else
        public async Task ExecuteAsync(IAdoCommandBuilder builder, Func<DbDataReader, Task> consumer, CancellationToken cancellationToken)
        {
            DbConnection connection = await this.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using DbCommand dbCommand = connection.CreateCommand();

            try
            {
                builder.Build(dbCommand);
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred building the ADO.NET command object.", ex);
            }

            this.ApplyCommandFilters(h => h.OnCommandCreated, dbCommand);

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            DbDataReader reader = null;

            try
            {
                reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.ApplyCommandFilters(h => h.OnCommandException, dbCommand, ex);

                throw new AdoException("An error occurred executing the ADO.NET command.", ex);
            }

            try
            {
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    await consumer(reader).ConfigureAwait(false);

                } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                reader?.Dispose();
            }

            this.ApplyCommandFilters(h => h.OnCommandExecuted, dbCommand);
        }
#endif

        private void CreateConnectionObject()
        {
            if (this.wasDisposed)
                throw new InvalidOperationException("Connection was disposed.");

            if (this.connection == null)
            {
                this.connection = this.connectionFactory();
                this.connectionAsync = this.connection as DbConnection;
            }
        }

        private IDbConnection GetOpenConnection()
        {
            this.CreateConnectionObject();

            if (this.connection.State == ConnectionState.Open)
                return this.connection;

            this.ApplyConnectionFilters(h => h.OnConnectionOpening);

            if (this.connection.State == ConnectionState.Broken)
                this.connection.Close();

            if (this.connection.State == ConnectionState.Closed)
                this.connection.Open();

            this.ApplyConnectionFilters(h => h.OnConnectionOpened);

            return this.connection;
        }

        private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            this.CreateConnectionObject();

            if (this.connectionAsync == null)
                this.ThrowAsyncNotAvailableException();
            else if (this.connectionAsync.State == ConnectionState.Open)
                return this.connectionAsync;

            this.ApplyConnectionFilters(h => h.OnConnectionOpening);

#if NETSTANDARD2_1
            if (this.connection.State == ConnectionState.Broken)
                await this.connectionAsync.CloseAsync();
#else
            if (this.connection.State == ConnectionState.Broken)
                this.connectionAsync.Close();
#endif

            if (this.connection.State == ConnectionState.Closed)
                await this.connectionAsync.OpenAsync(cancellationToken).ConfigureAwait(false);

            this.ApplyConnectionFilters(h => h.OnConnectionOpened);

            return this.connectionAsync;
        }

        private void ThrowAsyncNotAvailableException() => throw new AdoException("Async operations are only available for DbConnection instances.");

        private void ApplyCommandFilters(Func<IFilterHandler, Action<AdoCommandContext>> action, IDbCommand command, Exception exception = null)
            => this.ApplyFilters(h => action(h)(new AdoCommandContext(command, exception)));

        private void ApplyConnectionFilters(Func<IFilterHandler, Action<AdoConnectionContext>> action, Exception exception = null)
            => this.ApplyFilters(h => action(h)(new AdoConnectionContext(this.connection, exception)));

        private void ApplyFilters(Action<IFilterHandler> action)
        {
            foreach (IFilterHandler filter in this.filters)
                action(filter);
        }

        public void Dispose()
        {
            this.wasDisposed = true;

            if (this.connection != null)
            {
                try
                {
                    this.ApplyConnectionFilters(h => h.OnConnectionClosing);
                }
                finally
                {
                    this.connection.Dispose();
                }

                this.ApplyConnectionFilters(h => h.OnConnectionClosed);

                this.ApplyFilters(h => h.Dispose());
            }
        }
#if NETSTANDARD2_1
        public async ValueTask DisposeAsync()
        {
            this.wasDisposed = true;

            if (this.connection != null)
            {
                try
                {
                    this.ApplyConnectionFilters(h => h.OnConnectionClosing);
                }
                finally
                {
                    if (this.connectionAsync != null)
                        await this.connectionAsync.DisposeAsync();
                }

                this.ApplyConnectionFilters(h => h.OnConnectionClosed);

                this.ApplyFilters(h => h.Dispose());
            }
        }
#endif
    }
}
