if (-Not (Get-Command docker -errorAction SilentlyContinue))
{
    Install-PackageProvider -Name NuGet -Force
    Install-Module -Name DockerMsftProvider -Repository PSGallery -Force
    Install-Package -Name docker -ProviderName DockerMsftProvider -Force
    Restart-Computer -Wait -Force
}

$images = docker images | Out-String

if (-Not ($images -like "*microsoft/nanoserver*"))
{
    docker pull microsoft/nanoserver
}

if (-Not (Test-Path nodejs))
{
    wget -Uri https://nodejs.org/dist/v4.6.0/node-v4.6.0-x64.msi -OutFile node.msi -UseBasicParsing
    Start-Process -FilePath msiexec -ArgumentList /q, /i, node.msi -Wait
    Remove-Item -Path node.msi
    New-Item nodejs -type Directory
    Copy-Item '\Program Files\nodejs\node.exe' nodejs\node.exe
}

if (-Not ($images -like "*serverless-node*"))
{
    docker build -t serverless-node .
}
