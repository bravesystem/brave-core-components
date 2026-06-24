using BRaVe_CardPrinting_DesktopApp.Db;
using BRaVe_CardPrinting_DesktopApp.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;


public class PrintService
{
    public void Save(User user, string template, string printer)
    {
        using var db = DbConnectionFactory.Create();

        var sql = @"
        INSERT INTO PrintedRecords 
        ( FullName, HouseholdId, PrintedOn, TemplateUsed, PrinterName)
        VALUES 
        ( @FullName, @HouseholdId, @PrintedOn, @TemplateUsed, @PrinterName)";

        db.Execute(sql, new
        {
          
            FullName = user.FullName,
            HouseholdId = user.HouseholdId,
            PrintedOn = DateTime.Now,
            TemplateUsed = template,
            PrinterName = printer,
            //SyncTime = SyncTime
        });
    }

    public List<PrintedRecord> GetAll()
    {
        using var db = DbConnectionFactory.Create();

        return db.Query<PrintedRecord>(
            "SELECT * FROM PrintedRecords ORDER BY PrintedOn DESC"
        ).ToList();
    }
}