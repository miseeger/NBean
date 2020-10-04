@echo off
@echo ... stopping MySQL Server.
@C:\LARAGON\bin\mysql\mysql-5.7.24-winx64\bin\mysqladmin -u root -p shutdown
@echo MySQL Server is stopped.
exit
