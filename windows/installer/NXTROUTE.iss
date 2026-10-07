[Setup]
AppId={{BB08A7F3-BDBB-4F3C-8DA9-C1F170DC9461}
AppName=NXTROUTE
AppVersion=0.1.0
AppPublisher=NXTROUTE contributors
DefaultDirName={autopf}\NXTROUTE
DefaultGroupName=NXTROUTE
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
OutputDir=..\..\dist
OutputBaseFilename=NXTROUTE-Setup-0.1.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\..\LICENSE
UninstallDisplayIcon={app}\NXTROUTE.exe
CloseApplications=yes

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Files]
Source: "..\..\dist\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{commonappdata}\NXTROUTE"; Permissions: networkservice-modify

[Icons]
Name: "{group}\Apri NXTROUTE"; Filename: "http://127.0.0.1:3000"
Name: "{autodesktop}\NXTROUTE"; Filename: "http://127.0.0.1:3000"

[Run]
Filename: "http://127.0.0.1:3000"; Description: "Apri la dashboard NXTROUTE"; Flags: shellexec postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop NXTROUTE"; Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "{sys}\sc.exe"; Parameters: "delete NXTROUTE"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteService"

[Code]
function InitializeSetup(): Boolean;
begin
  if RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\NXTROUTE') then
  begin
    MsgBox('NXTROUTE è già installato. Questa prima versione non supporta aggiornamenti in-place: disinstallare prima il programma. I dati dei canali vengono conservati.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
  Result := True;
  MsgBox('Questa versione verifica il gateway con sorgenti sintetiche. SDI e NDI non sono ancora disponibili. Nessun driver Blackmagic o NDI Tools verrà installato o aggiornato.', mbInformation, MB_OK);
end;

procedure ServiceCommand(Parameters: String);
var
  ResultCode: Integer;
begin
  if not Exec(ExpandConstant('{sys}\sc.exe'), Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    RaiseException('Impossibile eseguire il gestore servizi Windows.');
  if ResultCode <> 0 then
    RaiseException('Configurazione del servizio NXTROUTE non riuscita. Codice Windows: ' + IntToStr(ResultCode));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    ServiceCommand('create NXTROUTE binPath= "\"' + ExpandConstant('{app}\NXTROUTE.exe') + '\"" start= auto obj= "NT AUTHORITY\NetworkService" DisplayName= "NXTROUTE Video Gateway"');
    ServiceCommand('failure NXTROUTE reset= 86400 actions= restart/5000/restart/15000/restart/60000');
    ServiceCommand('start NXTROUTE');
  end;
end;
