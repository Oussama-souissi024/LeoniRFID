# Script de génération des clés JWT pour Supabase Local
# Génère JWT_SECRET, ANON_KEY et SERVICE_ROLE_KEY compatibles avec Supabase

Add-Type -AssemblyName System.Security

# ── 1. Générer le JWT_SECRET (64 octets aléatoires en base64)
$secretBytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($secretBytes)
$JWT_SECRET = [Convert]::ToBase64String($secretBytes).Replace('+','').Replace('/','').Replace('=','').Substring(0,64)

Write-Host "JWT_SECRET=$JWT_SECRET"

# ── 2. Fonction pour créer un JWT HS256 signé
function New-JWT {
    param(
        [string]$Secret,
        [hashtable]$Payload
    )
    
    # Header
    $header = '{"alg":"HS256","typ":"JWT"}'
    $headerB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+','-').Replace('/','_')
    
    # Payload
    $payloadJson = $Payload | ConvertTo-Json -Compress
    $payloadB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson)).TrimEnd('=').Replace('+','-').Replace('/','_')
    
    # Signature HMAC-SHA256
    $signingInput = "$headerB64.$payloadB64"
    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $sigBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput))
    $sigB64 = [Convert]::ToBase64String($sigBytes).TrimEnd('=').Replace('+','-').Replace('/','_')
    
    return "$signingInput.$sigB64"
}

# ── 3. ANON_KEY (rôle: anon, expire dans 10 ans)
$exp = [int][double]::Parse((Get-Date -UFormat %s)) + (10 * 365 * 24 * 3600)
$anonPayload = @{
    role = "anon"
    iss  = "supabase"
    iat  = [int][double]::Parse((Get-Date -UFormat %s))
    exp  = $exp
}
$ANON_KEY = New-JWT -Secret $JWT_SECRET -Payload $anonPayload

# ── 4. SERVICE_ROLE_KEY (rôle: service_role, expire dans 10 ans)
$servicePayload = @{
    role = "service_role"
    iss  = "supabase"
    iat  = [int][double]::Parse((Get-Date -UFormat %s))
    exp  = $exp
}
$SERVICE_ROLE_KEY = New-JWT -Secret $JWT_SECRET -Payload $servicePayload

Write-Host ""
Write-Host "=========================================="
Write-Host "  Clés JWT générées pour Supabase Local"
Write-Host "=========================================="
Write-Host ""
Write-Host "JWT_SECRET=$JWT_SECRET"
Write-Host ""
Write-Host "ANON_KEY=$ANON_KEY"
Write-Host ""
Write-Host "SERVICE_ROLE_KEY=$SERVICE_ROLE_KEY"
Write-Host ""
Write-Host "=========================================="

# ── 5. Sauvegarder dans un fichier
$output = @"
JWT_SECRET=$JWT_SECRET
ANON_KEY=$ANON_KEY
SERVICE_ROLE_KEY=$SERVICE_ROLE_KEY
"@
$output | Out-File -FilePath "C:\supabase-keys.txt" -Encoding UTF8
Write-Host "✅ Clés sauvegardées dans C:\supabase-keys.txt"

# Retourner les valeurs pour utilisation dans d'autres scripts
return @{
    JWT_SECRET       = $JWT_SECRET
    ANON_KEY         = $ANON_KEY
    SERVICE_ROLE_KEY = $SERVICE_ROLE_KEY
}
