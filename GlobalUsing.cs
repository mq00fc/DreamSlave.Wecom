global using System.Net;
global using System.Xml;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Security.Cryptography;

global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using DreamSlave.Wecom.Services;
global using DreamSlave.Wecom.Models.Response;
global using DreamSlave.Wecom.Models.Response.UserInfo;
global using DreamSlave.Wecom.Models.WebDto;
global using DreamSlave.Wecom.Hosts;
global using DreamSlave.Wecom.Models.Request;