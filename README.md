## 企业微信相关方法封装

### 使用说明

本仓库目前为预览版，无任何使用保障，仅在部分项目中引用尚未发现任何问题，请勿用于生产环境



### Token/Ticket处理方式

当`cfg.AutoRefresh = true;`时，将在后台启动`BackgroundService`服务并在每60分钟自动刷新一次

> [!CAUTION]
>
> 由于未使用任何第三方库，所以采用了`IMemoryCache`即内存数据库，当每次程序重启时会自动刷新一次Token/Ticket，请注意遵守企业微信刷新规则限制，如有更高的需求请关闭自动刷新并自行实现此刷新逻辑（但是需要调用`RefreshAccessTokenAsync`和`RefreshJsApiTicketAsync`避免内部方法无法获取到Token的问题）





### 使用教程

该库提供了两种的注入方式方便使用者调用



#### 单一实例注入

```C#
builder.Services.AddWecomService(cfg =>
{
    cfg.CorpID = "企业微信Id";
    cfg.CorpSecret = "Secret";
    cfg.AgentId = 1000001;
    cfg.Token = "";
    cfg.EncodingAesKey = "";
    cfg.AutoRefresh = true;
});
```



#### 多实例注入（类似于IHttpClientFactory）

```C#
builder.Services.AddWecomService("名称",cfg =>
{
    cfg.CorpID = "企业微信Id";
    cfg.CorpSecret = "Secret";
    cfg.AgentId = 1000001;
    cfg.Token = "";
    cfg.EncodingAesKey = "";
    cfg.AutoRefresh = true;
});

```





在不同的实例注入下，使用方法有着本质的区别请注意

如果是单一实例，则直接注入`IWecomOAuth2Service`和`IWecomCallBackService`即可

```C#
private readonly ILogger<WecomController> _logger;
private readonly IWecomOAuth2Service _wecomOauth2Service;
private readonly IWecomCallBackService _wecomCallBackService;
public WecomController(ILogger<WecomController> logger,
     IWecomOAuth2Service wecomOAuth2Service,
     IWecomCallBackService wecomCallBackService)
 {
     _logger = logger;
     _wecomCallBackService = wecomCallBackService;
     _wecomOauth2Service = wecomOAuth2Service;
 }
```



如果是多实例注入，则需要注入`IWecomFactory`然后获取指定名称的Service

```C#
private readonly ILogger<WecomController> _logger;
private readonly IWecomOAuth2Service _wecomTokenService;
private readonly IWecomFactory _wecomFactory;
private readonly IWecomCallBackService _wecomCallBackService;
private readonly IWecomCallBackService _addressCallBackService;
public WecomController(ILogger<WecomController> logger,
    IWecomFactory wecomFactory)
{
    _logger = logger;
    _wecomFactory = wecomFactory;
    _wecomCallBackService = _wecomFactory.GetCallback("oauth2");
    _wecomTokenService = _wecomFactory.GetOAuth2("oauth2");
    _addressCallBackService = _wecomFactory.GetCallback("adress");
}
```

