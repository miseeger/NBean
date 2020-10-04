@echo off
@echo Stopping SQL Servers ...
@echo.
@start mssql_stop.cmd
@start pgsql_stop.cmd
@start mysql_stop.cmd
