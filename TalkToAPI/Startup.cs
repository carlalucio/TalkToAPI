using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToAPI.Database;
using TalkToAPI.V1.Repositories.Contracts;
using TalkToAPI.V1.Repositories;
using Microsoft.AspNetCore.Mvc.Formatters;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TalkToAPI.V1.Models;
using TalkToAPI.Helpers.Swagger;
using AutoMapper;
using TalkToAPI.Helpers;
using TalkToAPI.Helpers.Constantes;
using System.Xml.Serialization;

namespace TalkToAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //o AutoMapper é um biblioteca que mapeia um objeto de uma classe para outro 
            #region AutoMapper-Config
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new DTOMapperProfile()); //a classe DTOMapperProfile é onde cfg os mapeamentos que queremos fazer
            });
            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);
            #endregion

            //configuração para suprimir a validação do ModelState pelo Controller
            services.Configure<ApiBehaviorOptions>(op => {
                op.SuppressModelStateInvalidFilter = true;
            });

            //indica para o controller que a interface é que vai injetar a dependencia do repository
            services.AddScoped<IMensagemRepository, MensagemRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();           
            services.AddScoped<ITokenRepository, TokenRepository>();

            //configuração do banco de dados
            services.AddDbContext<TalkToContext>(cfg => {
                cfg.UseSqlite("Data Source=Database\\TalkTo.db");
            });

            //politica para autorizar o CORS
            services.AddCors(cfg =>
            {
                //adiciona uma politica padrão para acesso aos métodos da controller
                cfg.AddDefaultPolicy(policy =>
                {
                    //adiciona a origem domínio e os métodos autorizado e o que tem q conter no cabeçalho da requisição
                    policy
                        .WithOrigins("https://localhost:44387", "http://localhost:44387")
                        //.WithMethods("GET")  -> determina que somente reuisições do tipo GET serão aceitas 
                        //.WithHeaders("Accept", "Authorization")  -> libera acesso somente se no cabeçalho constar o formato da resposta e o token
                        .AllowAnyMethod()  //libera acesso a todos os métodos
                        .AllowAnyHeader()  //liberar todos os cabeçalhos
                        .SetIsOriginAllowedToAllowWildcardSubdomains(); //habilita o cors para todos os subdominios

                });
                //criar uma politica que habilita todos os sites para acessar a API, com restrição
                cfg.AddPolicy("AnyOrigin", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .WithMethods("GET")
                        .AllowAnyHeader();
                });
            });
            services.AddMvc(cfg =>
            {
                cfg.ReturnHttpNotAcceptable = true;                                  //retorna erro 406 se colocar um formato não suportado
                cfg.InputFormatters.Add(new XmlSerializerInputFormatter(cfg));    //formato da requisição suportada
                cfg.OutputFormatters.Add(new XmlSerializerOutputFormatter());        //formato da resposta suportada

                //configurando nosso prórpio tipo de mediaType
                //pega o formato json
                var jsonOutpuFormatter =  cfg.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();

                //se ele não for nulo:
                if(jsonOutpuFormatter != null)
                {
                    //o vnd permite gerar o mediatype do seu próprio tipo, indicando que vc aceita o formato json e o formato talkto
                    //o formato talkto.hateoas+json vai retonrar os hyperlinks em foramto json, qlquer outro formato de solicitação não retona os links
                    //pega o formato da classe customMediaType
                    jsonOutpuFormatter.SupportedMediaTypes.Add(CustomMediaType.Hateoas);
                }

            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
              .AddJsonOptions(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore 
                    ); 

            
            
            //adicionar servico de versaionamento de API
            services.AddApiVersioning(cfg =>
            {
                //essa função retorna no cabeçalho quais as versões suportadas e disponíveis quando for feita uma requisição
                cfg.ReportApiVersions = true;

                //cfg.ApiVersionReader = new HeaderApiVersionReader("api-version"); //adiciona o leitor de versão pelo cabeçalho da requisão
                cfg.AssumeDefaultVersionWhenUnspecified = true;  //direciona o usuário para a versão padrão caso não seja especificado isso na url
                cfg.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0); //indica a versão padrão 
            });

            //configurando o Swagger
            services.AddSwaggerGen(cfg =>
            {
                //configuração para o swagger habilitar a inclusão do Token na request - usar o JWT
                cfg.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    In = "header", //onde está localizado o processo de autenticação
                    Type = "apiKey",                                                  
                    Description = "Adicione o JSON Web Token(JWT) para autenticar.",
                    Name = "Authorization"
                });


                //cria um dicionário para receber o token(array de string) que será colocado no campo "Authorize"
                var security = new Dictionary<string, IEnumerable<string>>() {
                    {"Bearer", new string[] { } }                                 
                };
                cfg.AddSecurityRequirement(security);


                cfg.ResolveConflictingActions(apiDescription => apiDescription.First());//se tiver conflito de rota ele pega o primeiro

                //1º coloca a versão, 2º instancia classe Info() e coloca parametros {titulo, versao}            
                cfg.SwaggerDoc("v1.0", new Swashbuckle.AspNetCore.Swagger.Info()
                {
                    Title = "TalkToAPI - V1.0",
                    Version = "v1.0"
                });


                //criando variáveis que usam o platformServices para pegar o caminho e o nome do arquivo xml com os comentários
                var CaminhoProjeto = PlatformServices.Default.Application.ApplicationBasePath;
                var NomeProjeto = $"{PlatformServices.Default.Application.ApplicationName}.xml"; 
                var CaminhoAquivoXMLComentario = Path.Combine(CaminhoProjeto, NomeProjeto);

                //configuração para o swagger usar os comentários feitos no controller
                cfg.IncludeXmlComments(CaminhoAquivoXMLComentario);


                //configuração para selecionar qual versão quer exibir
                cfg.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
                    // would mean this action is unversioned and should be included everywhere
                    if (actionApiVersionModel == null)
                    {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
                });
                cfg.OperationFilter<ApiVersionOperationFilter>(); //passa a classe que criamos na Pasta Swagger, para filtrar por versão selecionada
            });

            // adiciona o Iconfiguration para acessar o arquivo de configuração e o JWT ter acesso a chave
            services.AddSingleton<IConfiguration>(Configuration);

            //adiciona o serviço de redirecionamento para autenticação por login pelo Identity 
            services.AddIdentity<ApplicationUser, IdentityRole>(options=>  
            {
                //desativar critérios de validação para a senha
                options.Password.RequireDigit = false; //remove a necessidade de ter digitos
                options.Password.RequiredLength = 5; //tamanho da senha
                options.Password.RequireLowercase = false; //letras minusculas
                options.Password.RequireUppercase = false; //letras maiusculas
                options.Password.RequireNonAlphanumeric = false; //caracteres especiais
                
            })
                    .AddEntityFrameworkStores<TalkToContext>()
                    .AddDefaultTokenProviders(); //habilita o uso de tokens ao invés de cookies



            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;//schema de autenticação padrão do jwt
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters() //quais parametros de um token vai validar 
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])) //pega a chave do aquivo appsettings.json

                };
            });

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                                                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme) //inidca qual schema de verificação
                                                .RequireAuthenticatedUser()         //verifica o usuario
                                                .Build()
                );
            });

            //adiciona  o tratamento da tela de redirecionamento de login, caso o usuario tente fazer uma solicitação sem autorização
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseStatusCodePages();
            app.UseAuthentication(); //indica a utilização do jwt token para ações com autorização
            app.UseHttpsRedirection(); //redireciona a requisição para HTTPS
            // app.UseCors("AnyOrigin"); //Desabilite quando for usar Atributos EnableCors/DisableCors direto nos controllers

            app.UseMvc();
            
           
           
                            
            app.UseSwagger(); // cria o arquivo base --> /swagger/v1/swagger.json

            //gera a interface gráfica do Swagger passando qual será o endpoint e o nome da API
            app.UseSwaggerUI(cfg =>
            {
                cfg.SwaggerEndpoint("/swagger/v1.0/swagger.json", "TalkToAPI V1.0");
                cfg.RoutePrefix = String.Empty; //configuração para que ao acessar a raiz da api direcione para o swaggerUI
            });
        }
    }
}
