#define ProductName "电极帽缺陷检测"
#define BatchRoot SourcePath + "\.."
[Setup]
AppId={{E38CA144-9F6A-4980-BB09-AC7CB6FB3030}
AppName={#ProductName}
AppVersion=2026.10.20.3
AppVerName={#ProductName} RC03 安装候选包
VersionInfoVersion=2026.10.20.3
DefaultDirName={autopf}\ElectrodeDet
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
SetupArchitecture=x64
MinVersion=10.0.17763
OutputDir={#BatchRoot}\Output
OutputBaseFilename=电极帽缺陷检测_Setup
Compression=lzma2/normal
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\App\电极帽缺陷检测.exe
AppMutex=Global\VisionBasedLaser_LaserRemoveGlue
CloseApplications=no
RestartApplications=no
SetupLogging=yes
UsePreviousAppDir=yes
DisableDirPage=no
Uninstallable=yes

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; Flags: checkedonce

[Dirs]
Name: "{commonappdata}\ElectrodeDet"; Permissions: users-modify; Flags: uninsneveruninstall
Name: "{commonappdata}\ElectrodeDet\Logs\Launcher"; Permissions: users-modify; Flags: uninsneveruninstall
Name: "{commonappdata}\ElectrodeDet\Logs\Installer"; Permissions: users-modify; Flags: uninsneveruninstall

[Files]
Source: "{#BatchRoot}\Input\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "RuntimePaths.json"
Source: "{#BatchRoot}\Input\Prerequisites\Configure-Runtime.ps1"; Flags: dontcopy
Source: "{#BatchRoot}\Input\Prerequisites\RuntimeSupport.ps1"; Flags: dontcopy
Source: "{#BatchRoot}\Input\Prerequisites\GPU50-runtime-lock.json"; Flags: dontcopy

[Icons]
Name: "{group}\{#ProductName}"; Filename: "{app}\Launch.cmd"; WorkingDir: "{app}\App"; IconFilename: "{app}\App\电极帽缺陷检测.exe"
Name: "{commondesktop}\{#ProductName}"; Filename: "{app}\Launch.cmd"; WorkingDir: "{app}\App"; IconFilename: "{app}\App\电极帽缺陷检测.exe"; Tasks: desktopicon

[UninstallDelete]
Type: files; Name: "{app}\RuntimePaths.json"
Type: files; Name: "{app}\RuntimePaths.json.bak"
Type: files; Name: "{app}\RuntimePaths.json.new"

[Code]
var
  RuntimePage: TInputQueryWizardPage;
  CandidatePage: TInputOptionWizardPage;
  MediaButton: TNewButton;
  InstallValidated: Boolean;

function Quote(Value: String): String;
begin
  if Pos('"', Value) > 0 then RaiseException('路径不得包含引号');
  // Avoid trailing backslash before a quoted argument's closing quote.
  if (Length(Value) > 3) and (Value[Length(Value)] = '\') then Delete(Value, Length(Value), 1);
  Result := '"' + Value + '"';
end;

function AllowMissing: Boolean;
begin
  Result := CandidatePage.Values[0] or (ExpandConstant('{param:ALLOWMISSINGDEPENDENCIES|0}') = '1');
end;

procedure OpenMedia(Sender: TObject);
var Code: Integer;
begin
  ShellExec('open', ExpandConstant('{src}\Prerequisites'), '', '', SW_SHOWNORMAL, ewNoWait, Code);
end;

procedure InitializeWizard;
begin
  CandidatePage := CreateInputOptionPage(wpSelectDir, '环境检查与候选包', '安装器只检测环境，不自动修改厂商驱动或商业授权。',
    '建议先安装离线目录 Prerequisites 中匹配的厂商介质，再继续。缺项时默认阻止安装。', False, False);
  CandidatePage.Add('仅安装软件候选包：允许环境缺项，后续补齐后重新运行 Setup；本次不得视为整机验收通过');
  CandidatePage.Values[0] := False;
  MediaButton := TNewButton.Create(WizardForm);
  MediaButton.Parent := CandidatePage.Surface;
  MediaButton.SetBounds(0, ScaleY(100), ScaleX(220), ScaleY(30));
  MediaButton.Caption := '打开同目录 Prerequisites';
  MediaButton.OnClick := @OpenMedia;
  RuntimePage := CreateInputQueryPage(CandidatePage.ID, 'GPU 运行库路径', '留空自动识别，或填写厂商安装的实际路径。',
    'CUDA 12.8.0 根目录（包含 version.json）；cuDNN 9.14 CUDA12 的 DLL 目录。安装时逐项核对版本与哈希，不改变全局 PATH。');
  RuntimePage.Add('CUDA 根目录（可留空）：', False);
  RuntimePage.Add('cuDNN DLL 目录（可留空）：', False);
  RuntimePage.Values[0] := ExpandConstant('{param:CUDAROOT|}');
  RuntimePage.Values[1] := ExpandConstant('{param:CUDNNBIN|}');
end;

function RunConfiguration(Apply: Boolean; ReportDir: String; var ExitCode: Integer): Boolean;
var ScriptPath, Args: String;
begin
  if Apply then ScriptPath := ExpandConstant('{app}\Prerequisites\Configure-Runtime.ps1')
  else ScriptPath := ExpandConstant('{tmp}\Configure-Runtime.ps1');
  Args := '-NoLogo -NoProfile -ExecutionPolicy Bypass -File ' + Quote(ScriptPath) +
    ' -InstallRoot ' + Quote(ExpandConstant('{app}')) + ' -ReportDirectory ' + Quote(ReportDir);
  if RuntimePage.Values[0] <> '' then Args := Args + ' -CudaRoot ' + Quote(RuntimePage.Values[0]);
  if RuntimePage.Values[1] <> '' then Args := Args + ' -CudnnBin ' + Quote(RuntimePage.Values[1]);
  if AllowMissing then Args := Args + ' -AllowMissingDependencies';
  if Apply then Args := Args + ' -Apply';
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Args, '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer; Summary: AnsiString;
begin
  Result := '';
  if CheckForMutexes('Global\VisionBasedLaser_LaserRemoveGlue') then begin
    Result := '检测程序仍在运行。请正常退出后重试。'; Exit;
  end;
  ExtractTemporaryFile('Configure-Runtime.ps1');
  ExtractTemporaryFile('RuntimeSupport.ps1');
  ExtractTemporaryFile('GPU50-runtime-lock.json');
  WizardForm.StatusLabel.Caption := '正在检测环境并校验 GPU 组件，请稍候…';
  if not RunConfiguration(False, ExpandConstant('{tmp}\ElectrodeDet-check'), Code) then
    Result := '无法启动 Windows PowerShell 环境检查。'
  else if Code <> 0 then begin
    LoadStringFromFile(ExpandConstant('{tmp}\ElectrodeDet-check\dependency-summary.txt'), Summary);
    Result := '环境检查未通过。请补齐匹配的厂商组件后重试。' + #13#10 + Summary;
    if Code = 1 then Result := Result + #13#10 + '检查脚本执行失败，查看安装日志和 configure-error.txt。';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Code: Integer; ReportDir: String;
begin
  if CurStep = ssPostInstall then begin
    InstallValidated := False;
    ReportDir := ExpandConstant('{commonappdata}\ElectrodeDet\Logs\Installer\') + GetDateTimeString('yyyymmdd_hhnnss', '-', ':');
    if not RunConfiguration(True, ReportDir, Code) then RaiseException('无法生成本机 RuntimePaths.json');
    if Code <> 0 then RaiseException('静态文件或环境检查失败，安装未验证完成。报告：' + ReportDir);
    InstallValidated := True;
  end;
end;

function GetCustomSetupExitCode: Integer;
begin
  if InstallValidated then Result := 0 else Result := 10;
end;
