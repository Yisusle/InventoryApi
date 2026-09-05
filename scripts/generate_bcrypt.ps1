param(
    [Parameter(Mandatory=$false)]
    [string]$Password
)

if (-not $Password)
{
    Write-Host "Enter password to hash:" -NoNewline
    $secure = Read-Host -AsSecureString
    $Ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($Ptr)
}

try {
    # Try to use loaded BCrypt.Net assembly if available
    $hash = [BCrypt.Net.BCrypt]::HashPassword($Password)
    Write-Host "BCrypt hash:`n$hash"
}
catch {
    Write-Host "Unable to compute hash in this environment. If you have dotnet installed, use:"
    Write-Host "  dotnet add package BCrypt.Net-Next --version 4.0.2"
    Write-Host "  then run a small C# snippet to generate the hash, or run the script inside a dotnet script context."
}
