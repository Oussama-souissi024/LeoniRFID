# Script de configuration du .env Supabase Local
# A exécuter après le git clone, depuis C:\Users\oussa\supabase-local\docker\

$JWT_SECRET       = "IiNFs2gjFHvG7I4FPBqBGqSgXAlVmq8zTQlZWMRta4U3fSf7axln98KMU6sfgbpr"
$ANON_KEY         = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoiYW5vbiJ9.9Ih9WSOMJxCeTSzYUmOTNYPcsm82suLqON5DeO0qgaI"
$SERVICE_ROLE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzY3MDY3MjUsImlzcyI6InN1cGFiYXNlIiwiZXhwIjoyMDkyMDY2NzI1LCJyb2xlIjoic2VydmljZV9yb2xlIn0.IPwblUTo1d53u6Zo48EaOgGqlfxb6xHwyDunhMYVo5E"
$POSTGRES_PASS    = "LeoniRFID2024Prod"
$LOCAL_IP         = "192.168.1.122"

$envPath = "C:\Users\oussa\supabase-local\docker\.env"

if (-not (Test-Path $envPath)) {
    Write-Error "❌ .env.example introuvable. Vérifier que le clone est terminé."
    exit 1
}

Write-Host "📝 Configuration du .env Supabase Local..."

# Lire le contenu
$content = Get-Content $envPath -Raw

# Remplacer les valeurs clés
$content = $content -replace 'POSTGRES_PASSWORD=.*',         "POSTGRES_PASSWORD=$POSTGRES_PASS"
$content = $content -replace 'JWT_SECRET=.*',                "JWT_SECRET=$JWT_SECRET"
$content = $content -replace 'ANON_KEY=.*',                  "ANON_KEY=$ANON_KEY"
$content = $content -replace 'SERVICE_ROLE_KEY=.*',          "SERVICE_ROLE_KEY=$SERVICE_ROLE_KEY"
$content = $content -replace 'SITE_URL=.*',                  "SITE_URL=http://${LOCAL_IP}:8000"
$content = $content -replace 'API_EXTERNAL_URL=.*',          "API_EXTERNAL_URL=http://${LOCAL_IP}:8000"
$content = $content -replace 'SUPABASE_PUBLIC_URL=.*',       "SUPABASE_PUBLIC_URL=http://${LOCAL_IP}:8000"
$content = $content -replace 'DASHBOARD_PASSWORD=.*',        "DASHBOARD_PASSWORD=LeoniAdmin2024"

$content | Set-Content $envPath -Encoding UTF8 -NoNewline

Write-Host "✅ .env configuré avec succès !"
Write-Host "   - POSTGRES_PASSWORD: $POSTGRES_PASS"
Write-Host "   - JWT_SECRET: $($JWT_SECRET.Substring(0,20))..."
Write-Host "   - SITE_URL: http://${LOCAL_IP}:8000"
Write-Host "   - DASHBOARD_PASSWORD: LeoniAdmin2024"
