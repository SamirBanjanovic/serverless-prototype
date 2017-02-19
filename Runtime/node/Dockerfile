FROM microsoft/nanoserver

ADD nodejs/node.exe /node.exe
ADD serverless.js /

CMD mkdir app && xcopy \function \app /s /e /y && node.exe serverless.js
