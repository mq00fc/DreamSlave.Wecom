## 企业微信相关方法封装

### 状态
当前为预览版，仅用于实验/学习，勿直接用于生产。

### 功能
- [x] AccessToken 获取 / 缓存(IMemoryCache)
- [x] Jsapi Ticket 获取 / 缓存(IMemoryCache)
- [x] 回调签名验证 / 加解密
- [x] 回调消息解析 (文本/事件/扫码/批量任务/通讯录变更等)
- [x] OAuth2 鉴权 & 用户基础/敏感信息
- [x] Web 登录鉴权
- [x] 应用消息发送(文本/Markdown/图片/语音/视频/卡片/图文等)

```
IWecomUnifiedService
```

通过 serviceName 区分多实例，内部使用 IMemoryCache 保存 token / ticket。

### 注册方式
```
// 多实例按需调用多次
builder.Services.AddWecomConfig("oauth2", cfg =>
{
    cfg.CorpID = "企业微信Id";
    cfg.CorpSecret = "Secret";
    cfg.AgentId = 1000001;
    cfg.Token = "";
    cfg.EncodingAesKey = "";
    cfg.AutoRefresh = true; // 后台自动刷新
});
```

### 获取 Js-SDK 签名
```
var dto = _wecom.GetJsapiTicketDto("oauth2", currentUrl);
```

### OAuth2 鉴权
```
var authUrl = _wecom.BuildOAuth2Url("oauth2", callbackUrl, state);
// 回调里
var user = await _wecom.GetOAuth2UserInfoAsync("oauth2", code);
var detail = await _wecom.GetOAuth2UserDetailAsync("oauth2", user);
```

### Web 登录
```
var loginUrl = _wecom.BuildWebLoginUrl("oauth2", callbackUrl, state);
var webUser = await _wecom.GetWebLoginUserInfoAsync("oauth2", code);
```

### 回调处理
后台已自动注册 WecomCommandExecService (按实例 serviceName)。
```
[HttpGet("/api/wecom/callback")] // 检测签名
public IActionResult Verify([FromQuery] Callback cb)
{
    if(!_wecom.CheckSignature("oauth2", cb)) return Content("签名错误");
    return Content(_wecom.DecryptEchostr("oauth2", cb));
}

[HttpPost("/api/wecom/callback")] // 处理消息
public async Task<IActionResult> Receive([FromQuery] Callback cb)
{
    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();
    var xml = _wecom.DecryptCallBackData("oauth2", payload);
    return Content(await _exec.HandleMessageAsync(xml));
}
```

### 自定义指令/事件处理
文本命令:
```
public class DeveloperHandler
{
    [WecomTextCommand("id")]
    public Task<string> IdAsync(IServiceProvider sp, MessageReceive msg)
        => Task.FromResult("您的ID:" + msg.fromUserName);
}
```
事件命令:
```
public class EventHandler
{
    [WecomEventCommand("enter_agent")] public Task<string> EnterAsync(IServiceProvider sp, MessageReceive m) => Task.FromResult("进入应用:"+m.AgentId);
}
```
支持作用域过滤:
```
[WecomHandlerScope("oauth2")] // 仅在 oauth2 实例生效
```

### 发送消息
```
await _wecom.SendTextMessageAsync("oauth2", "测试", "UserA");
```

### 自动刷新
当配置 AutoRefresh=true，会启动后台刷新服务：
- AccessToken 提前 1 分钟过期续订
- JsapiTicket 需自行按需调用刷新 (或调用 GetJsapiTicketDto 前确保已刷新)

### 免责声明
仅供学习/测试，不对稳定性与合规性负责。

