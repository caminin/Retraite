@echo off
rem Exécuté une fois à la fin de l'installation de Windows (dockur copie ce dossier dans C:\OEM).
powershell -NoProfile -ExecutionPolicy Bypass -File C:\OEM\install.ps1 > C:\OEM\install.log 2>&1
