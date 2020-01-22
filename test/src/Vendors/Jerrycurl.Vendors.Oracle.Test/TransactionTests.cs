﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Filters;
using Jerrycurl.Data.Queries;
using Jerrycurl.Test;
using Shouldly;

namespace Jerrycurl.Vendors.Oracle.Test
{
    public class TransactionTests
    {
        private readonly TransactionHelper helper = new TransactionHelper(() => OracleConvention.GetConnection(), CreateSql, InsertSql, SelectSql);

        private const string InsertSql = @"BEGIN
                                                INSERT INTO ""tran_values"" VALUES(1);
                                                INSERT INTO ""tran_values"" VALUES(2);
                                                INSERT INTO ""tran_values"" VALUES(NULL);
                                           END;";
        private const string SelectSql = @"SELECT ""v"" AS ""Item"" FROM ""tran_values""";
        private const string CreateSql = @"BEGIN
                                              BEGIN
                                                  EXECUTE IMMEDIATE 'DROP TABLE ""tran_values""';
                                              EXCEPTION
                                                  WHEN OTHERS THEN NULL;
                                              END;
                                              BEGIN
                                                  EXECUTE IMMEDIATE 'CREATE TABLE ""tran_values"" ( ""v"" NUMBER NOT NULL )';
                                              EXCEPTION
                                                  WHEN OTHERS THEN NULL;
                                              END;
                                           END;";

        public void Test_Inserts_WithImplicitTransaction()
        {
            this.helper.CreateTable();

            CommandData command = this.helper.GetInsert();
            CommandHandler handler = this.helper.GetCommandHandler();

            try
            {
                handler.Execute(command);
            }
            catch (DbException) { }

            this.helper.VerifyTransaction();
        }

        public async Task Test_InsertsAsync_WithImplicitTransaction()
        {
            this.helper.CreateTable();

            CommandData command = this.helper.GetInsert();
            CommandHandler handler = this.helper.GetCommandHandler();

            try
            {
                await handler.ExecuteAsync(command);
            }
            catch (DbException) { }

            this.helper.VerifyTransaction();
        }
    }
}
