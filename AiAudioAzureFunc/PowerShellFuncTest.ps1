Invoke-RestMethod -Uri "http://localhost:7246/api/ProxyFunction" `
  -Method POST `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body '{
        "model": "gpt-3.5-turbo",
        "messages": [{"role": "user", "content": "Tell me a joke"}]
      }'