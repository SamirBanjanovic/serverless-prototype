const handler = require("/app/index.js");

const http = require('http');

const requestHandler = (request, response) => {
    var input = JSON.parse(request.body);
    var output = handler(input);
    response.end(JSON.stringify(output));
};

const server = http.createServer(requestHandler);

server.listen(8080);
