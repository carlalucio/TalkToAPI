using TalkToAPI.Database;
using TalkToAPI.V1.Models;
using TalkToAPI.V1.Repositories.Contracts;
using System.Linq;

namespace TalkToAPI.V1.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private readonly TalkToContext _banco;
        public TokenRepository(TalkToContext banco)
        {
            _banco = banco;
        }
        public Token Obter(string refreshToken)
        {
           return  _banco.Tokens.FirstOrDefault(a => a.RefreshToken == refreshToken && a.Utilizado == false);
        }

        public void Cadastrar(Token token)
        {
            _banco.Tokens.Add(token);
            _banco.SaveChanges();
        }
        public void Atualizar(Token token)
        {
            _banco.Tokens.Update(token);
            _banco.SaveChanges();
        }

        

        
    }
}
