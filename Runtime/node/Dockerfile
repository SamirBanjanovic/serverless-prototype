FROM microsoft/nanoserver

ADD node.exe /node.exe
ADD serverless.js /

CMD mkdir app && xcopy \function \app /s /e /y && node.exe serverless.js
