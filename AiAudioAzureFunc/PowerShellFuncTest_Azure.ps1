Invoke-RestMethod -Uri "http://auidioai-fa-c5a0cvcjepgrandy.westus2-01.azurewebsites.net" `
  -Method POST `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body '{
        "model": "gpt-3.5-turbo",
        "messages": [{"role": "user", "content": "Tell me a joke"}]
      }'