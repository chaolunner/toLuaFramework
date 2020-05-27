@echo off

set PROTOC=.\protoc-3.12.1-win64\bin\protoc.exe
set PB_OUT=..\Assets\Proto

rd /S /Q %PB_OUT%
md %PB_OUT%

for /r %%i in (protos\*.proto) do (
  %PROTOC% --proto_path=%%~di%%~pi -o %PB_OUT%\%%~ni.pb%1 %%i
)
