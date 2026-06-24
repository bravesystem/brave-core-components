package intl.iom.bravemobile.database;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteStatement;

import java.sql.SQLDataException;

public class DatabaseManager {

    private DatabaseHelper dbHelper;
    private Context context;
    private SQLiteDatabase database;

    public DatabaseManager(Context context)
    {
        this.context = context;
    }

    public DatabaseManager open() throws SQLDataException{
        dbHelper = new DatabaseHelper(context);
        database = dbHelper.getWritableDatabase();
        return this;
    }

    public void close(){
        dbHelper.close();
    }

    public void beginTransaction(){
        database.beginTransaction();
    }
    public void endTransaction(){
        database.endTransaction();
    }
    public void setTransactionSuccessful(){
        database.setTransactionSuccessful();
    }
    public void execSQL(String query){
        database.execSQL(query);
    }

    public void execSQL(String query, String[] args){
        database.execSQL(query, args);
    }

    public int delete(String query,String whereClause, String[] args){
        return database.delete(query, whereClause, args);
    }

    public void insert(String query, ContentValues cv){
        database.insert(query, null, cv);
    }

    public long update(String table, ContentValues values, String conditions, String[] args)
    {
        return database.update(table, values, conditions, args);
    }

    public long replace(String table, ContentValues values)
    {
        return database.replace(table, null, values);
    }

    public SQLiteStatement compileStatement(String sql)
    {
        return database.compileStatement(sql);
    }
    public Cursor rawQuery(String query){
        return database.rawQuery(query, null);
    }

    public Cursor rawQuery(String query, String[] args){
        return database.rawQuery(query, args);
    }

    // db.insertWithOnConflict("tbl_households", null, cv, SQLiteDatabase.CONFLICT_REPLACE);

    public void insertWithOnConflict(String table, ContentValues cv){
        database.insertWithOnConflict(table, null, cv, SQLiteDatabase.CONFLICT_REPLACE);
    }



}
