const handler = require("/app/index.js");

var http = require('http');

http.createServer(function(request, response) {
  var body = [];
  request.on('error', function(err) {
    console.error(err);
  }).on('data', function(chunk) {
    body.push(chunk);
  }).on('end', function() {
    body = Buffer.concat(body).toString();
    var input = JSON.parse(body);
    var output = handler(input);
    response.end(JSON.stringify(output));
  });
}).listen(8080, () => {
  console.log('started');
});
