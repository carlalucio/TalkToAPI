using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using TalkToAPI.Database;
using TalkToAPI.V1.Models;
using TalkToAPI.V1.Repositories.Contracts;

namespace TalkToAPI.V1.Repositories
{
    public class MensagemRepository : IMensagemRepository
    {
        private readonly TalkToContext _banco;
        public MensagemRepository(TalkToContext banco)
        {
            _banco = banco;
                
        }
        public Mensagem Obter(int id)
        {
            return _banco.Mensagem.Find(id);
        }

        public List<Mensagem> ObterMensagens(string usuarioUmId, string usuarioDoisId)
        {
            return _banco.Mensagem.Where(a => (a.DeId == usuarioUmId || a.DeId == usuarioDoisId) && (a.ParaId == usuarioUmId || a.ParaId == usuarioDoisId)).ToList();
        }

        public void Cadastrar(Mensagem mensagem)
        {
            _banco.Mensagem.Add(mensagem);
            _banco.SaveChanges();
        }

        public void Atualizar(Mensagem mensagem)
        {
            _banco.Mensagem.Update(mensagem);
            _banco.SaveChanges();
        }

        
    }
}
