using System.Collections.Generic;
using TalkToAPI.V1.Models;

namespace TalkToAPI.V1.Repositories.Contracts
{
    public interface IMensagemRepository
    {
        Mensagem Obter(int id);
        List<Mensagem> ObterMensagens(string usuarioUmId, string usuarioDoisId);

        void Cadastrar(Mensagem mensagem);
        void Atualizar(Mensagem mensagem);

    }
}
