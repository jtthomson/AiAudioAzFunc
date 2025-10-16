$headers = @{ "Content-Type" = "application/json" }
$body = '{"msg":"Hello"}'

Invoke-RestMethod -Uri "http://localhost:7246/api/ProxyFunction" -Method Post -Headers $headers -Body $body