if (-Not (Get-Command docker -errorAction SilentlyContinue))
{
    Install-PackageProvider -Name NuGet -Force
    Install-Module -Name DockerMsftProvider -Repository PSGallery -Force
    Install-Package -Name docker -ProviderName DockerMsftProvider -Force
    Restart-Computer -Wait -Force
}

dockerd --unregister-service
net stop docker
dockerd -H npipe:////./pipe/docker_engine -H 0.0.0.0:2375 --register-service
net start docker

$images = docker images | Out-String

if (-Not ($images -like "*microsoft/nanoserver*"))
{
    docker pull microsoft/nanoserver
}

if (-Not (Test-Path node\node.exe))
{
    wget -Uri https://nodejs.org/dist/v6.9.5/node-v6.9.5-x64.msi -OutFile node.msi -UseBasicParsing
    Start-Process -FilePath msiexec -ArgumentList /q, /i, node.msi -Wait
    Remove-Item -Path node.msi
    Copy-Item '\Program Files\nodejs\node.exe' node\node.exe
}

if (-Not ($images -like "*serverless-node*"))
{
    cd node
    docker build -t serverless-node .
}
