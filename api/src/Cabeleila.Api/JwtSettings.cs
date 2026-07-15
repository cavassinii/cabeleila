namespace API
{
    // Configuracao do JWT como classe compilada (nao vem de appsettings.json), conforme solicitado.
    // Atencao: por estar embutida no binario, qualquer pessoa com acesso a DLL consegue extrair esta chave
    // (decompilacao trivial em .NET). Para producao com dados sensiveis, o ideal seria mover para uma
    // variavel de ambiente / secret manager. Mantido assim aqui por ser um requisito explicito do padrao do projeto.
    public static class JwtSettings
    {
        public const string Key = "wfgPPnhGeARFfIcg8Xbd4B83N0edMyQdeGc4dFcNalXE4cC47YGLdg/24H27z4eF5u8j0J7ewhTCr2NvutvDig==";
        public const string Issuer = "Cabeleila.Api";
        public const string Audience = "Cabeleila.Client";
        public const int ExpirationMinutes = 120;
    }
}
