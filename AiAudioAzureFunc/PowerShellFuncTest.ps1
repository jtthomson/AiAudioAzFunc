Invoke-RestMethod -Uri "http://localhost:7246/api/ProxyFunction" `
  -Method POST `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body 'Tell Me A Dad joke'