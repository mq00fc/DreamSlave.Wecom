namespace DreamSlave.Wecom.Services
{
    public class WecomCallBackService : IWecomCallBackService
    {
        private readonly ILogger<WecomCallBackService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<Models.Config> _options;
        public WecomCallBackService(
            ILogger<WecomCallBackService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<Models.Config> options)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options;
        }

        #region 企业微信方法
        private static int HostToNetworkOrder(int inval)
        {
            int outval = 0;
            for (int i = 0; i < 4; i++)
                outval = (outval << 8) + ((inval >> (i * 8)) & 255);
            return outval;
        }
        /// <summary>
        /// 解密方法
        /// </summary>
        /// <param name="input">密文</param>
        /// <param name="encodingAESKey"></param>
        /// <returns></returns>
        /// 
        private static string AesDecrypt(String input, string encodingAESKey, out string corpid)
        {
            // 解码 AES 密钥
            byte[] key = Convert.FromBase64String(encodingAESKey + "=");

            // 从 key 的前 16 个字节中获取初始化向量（IV）
            byte[] iv = key[..16];

            // 使用 AES 解密
            byte[] bTmpMsg = AES_decrypt(input, iv, key);

            // 获取消息长度
            int len = BitConverter.ToInt32(bTmpMsg, 16);
            len = IPAddress.NetworkToHostOrder(len);

            // 分割消息和公司 ID
            byte[] bMsg = bTmpMsg[20..(20 + len)];
            byte[] bCorpid = bTmpMsg[(20 + len)..];

            // 解码原始消息和公司 ID
            string oriMsg = Encoding.UTF8.GetString(bMsg);
            corpid = Encoding.UTF8.GetString(bCorpid);

            return oriMsg;
        }

        /// <summary>
        /// 加密方法
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encodingAESKey"></param>
        /// <param name="corpid"></param>
        /// <returns></returns>
        private static String AesEncrypt(String input, string encodingAESKey, string corpid)
        {
            // 解码 AES 密钥
            byte[] key = Convert.FromBase64String(encodingAESKey + "=");
            // 从 key 的前 16 个字节中获取初始化向量（IV）
            byte[] iv = key[..16];
            // 生成随机代码
            string randCode = CreateRandCode(16);
            byte[] bRand = Encoding.UTF8.GetBytes(randCode);
            byte[] bCorpid = Encoding.UTF8.GetBytes(corpid);
            byte[] bTmpMsg = Encoding.UTF8.GetBytes(input);
            byte[] bMsgLen = BitConverter.GetBytes(HostToNetworkOrder(bTmpMsg.Length));
            // 使用数组展开操作符合并所有部分
            byte[] bMsg = new byte[bRand.Length + bMsgLen.Length + bTmpMsg.Length + bCorpid.Length];
            bMsg = [.. bRand, .. bMsgLen, .. bTmpMsg, .. bCorpid];
            // 使用 AES 进行加密
            return AES_encrypt(bMsg, iv, key);

        }
        private static string CreateRandCode(int codeLen)
        {
            string codeSerial = "2,3,4,5,6,7,a,c,d,e,f,h,i,j,k,m,n,p,r,s,t,A,C,D,E,F,G,H,J,K,M,N,P,Q,R,S,U,V,W,X,Y,Z";
            if (codeLen == 0)
            {
                codeLen = 16;
            }
            string[] arr = codeSerial.Split(',');
            string code = "";
            int randValue = -1;
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < codeLen; i++)
            {
                randValue = rand.Next(0, arr.Length - 1);
                code += arr[randValue];
            }
            return code;
        }

        private static string AES_encrypt(byte[] input, byte[] iv, byte[] key)
        {
            using Aes aes = Aes.Create();
            // 秘钥的大小，以位为单位
            aes.KeySize = 256;
            // 支持的块大小
            aes.BlockSize = 128;
            // 填充模式
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.IV = iv;
            using var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            // 自己进行 PKCS7 补位
            byte[] msg = [.. input, .. KCS7Encoder(input.Length)];
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write);
            cs.Write(msg, 0, msg.Length);
            string output = Convert.ToBase64String(ms.ToArray());
            return output;
        }


        private static byte[] KCS7Encoder(int text_length)
        {
            int block_size = 32;
            // 计算需要填充的位数
            int amount_to_pad = block_size - (text_length % block_size);
            if (amount_to_pad == 0)
            {
                amount_to_pad = block_size;
            }
            // 获得补位所用的字符
            char pad_chr = chr(amount_to_pad);
            string tmp = "";
            for (int index = 0; index < amount_to_pad; index++)
            {
                tmp += pad_chr;
            }
            return Encoding.UTF8.GetBytes(tmp);
        }
        /**
         * 将数字转化成ASCII码对应的字符，用于对明文进行补码
         * 
         * @param a 需要转化的数字
         * @return 转化得到的字符
         */
        private static char chr(int a)
        {
            return (char)(byte)(a & 0xFF);
        }


        private static byte[] AES_decrypt(string input, byte[] iv, byte[] key)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = key;
            aes.IV = iv;
            using var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write);
            byte[] xXml = Convert.FromBase64String(input);
            byte[] msg = [.. xXml, .. new byte[32 - xXml.Length % 32]];
            cs.Write(xXml, 0, xXml.Length);
            return decode2(ms.ToArray());
        }


        private static byte[] decode2(byte[] decrypted)
        {
            int pad = (int)decrypted[^1];
            if (pad < 1 || pad > 32)
            {
                pad = 0;
            }
            byte[] res = decrypted[..(decrypted.Length - pad)];
            return res;
        }

        #endregion


        #region 公用方法
        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public bool CheckSignature(string token, Callback callback)
        {
            var sign = GenerateSignature(token, callback);
            return sign == callback.msg_signature;
        }


        /// <summary>
        /// 生成签名
        /// </summary>
        /// <param name="token"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string GenerateSignature(string token, Callback callback)
        {
            var values = new List<string> { token, callback.timestamp, callback.nonce, callback.echostr };
            values.Sort(StringComparer.Ordinal);

            string raw = string.Concat(values);
            using var sha = SHA1.Create();
            byte[] dataToHash = Encoding.ASCII.GetBytes(raw);
            byte[] dataHashed = sha.ComputeHash(dataToHash);
            return Convert.ToHexString(dataHashed).ToLowerInvariant();
        }

        /// <summary>
        /// 解密企业微信的echostr
        /// </summary>
        /// <param name="aesKey"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string DecryptEchostr(string aesKey, Callback callback)
        {
            if (!CheckSignature(callback))
            {
                return string.Empty;
            }
            var data = AesDecrypt(callback.echostr ?? "", aesKey, out string corpId);
            if (corpId != _options.Value.CorpID)
            {
                return string.Empty;
            }
            return data;
        }


        /// <summary>
        /// 解密企业微信的消息
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public string DecryptCallBackData(string payload, string aesKey, string corpId)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(payload);
                XmlNode root = xmlDocument.FirstChild!;

                string encryptData = root["Encrypt"]?.InnerText ?? "";

                //如果企业id不相符的话则返回空字符串
                var data = AesDecrypt(encryptData, aesKey, out string id);
                if (id != corpId)
                {
                    return string.Empty;
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError("企业微信解密数据失败!:{0}\n{1}\n{2}", ex.Message, ex.StackTrace, payload);
                return string.Empty;
            }
        }


        /// <summary>
        /// 加密企业微信回调数据
        /// </summary>
        /// <param name="payload">需要返回的内容</param>
        /// <returns></returns>
        public string EncryptCallBackData(string payload, string aesKey, string corpId)
        {
            try
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                var nonce = Guid.NewGuid().ToString("n");
                string encryptReplay = AesEncrypt(payload, aesKey, corpId);
                string replaySign = GenerateSignature(new Callback() { timestamp = timestamp, nonce = nonce, echostr = encryptReplay });


                StringBuilder sb = new StringBuilder(4096);
                sb.AppendLine("<xml>");
                sb.AppendLine($"\t<Encrypt><![CDATA[{encryptReplay}]]></Encrypt>");
                sb.AppendLine($"\t<MsgSignature><![CDATA[{replaySign}]]></MsgSignature>");
                sb.AppendLine($"\t<TimeStamp>{timestamp}</TimeStamp>");
                sb.AppendLine($"\t<Nonce><![CDATA[{nonce}]]></Nonce>");
                sb.AppendLine("</xml>");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError("企业微信加密数据失败!:{0}\n{1}\n{2}", ex.Message, ex.StackTrace, payload);
                return string.Empty;
            }
        }


        /// <summary>
        /// 发送文本类型的回调消息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string SendTextMessage(string userId, string content, string corpId)
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.AppendLine("<xml>");
            sb.AppendLine($"\t<ToUserName><![CDATA[{userId}]]></ToUserName>");
            sb.AppendLine($"\t<FromUserName><![CDATA[{corpId}]]></FromUserName>");
            sb.AppendLine($"\t<CreateTime>{DateTimeOffset.Now.ToUnixTimeSeconds()}</CreateTime>");
            sb.AppendLine("\t<MsgType><![CDATA[text]]></MsgType>");
            sb.AppendLine($"\t<Content><![CDATA[{content}]]></Content>");
            sb.AppendLine("</xml>");

            return EncryptCallBackData(sb.ToString());
        }


        /// <summary>
        /// 解析负载
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public MessageReceive ResolvePayload(string payload)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(payload);
                XmlNode root = xmlDocument.FirstChild!;

                var createTime = root["CreateTime"].InnerText ?? "0";
                var agentId = root["AgentID"].InnerText ?? "0";
                var model = new MessageReceive
                {
                    Payload = payload,
                    CreateTime = Convert.ToInt32(createTime),
                    AgentId = Convert.ToInt32(agentId),
                    MsgType = root["MsgType"]?.InnerText ?? "",
                    EventName = root["Event"]?.InnerText ?? "",
                    ChangeType = root["ChangeType"]?.InnerText ?? "",
                    fromUserName = root["FromUserName"]?.InnerText ?? "",
                    ToUserName = root["ToUserName"]?.InnerText ?? "",
                    ExpiredTime = root["ExpiredTime"]?.InnerText ?? "",
                    Latitude = root["Latitude"]?.InnerText ?? "",
                    Longitude = root["Longitude"]?.InnerText ?? "",
                    Precision = root["Precision"]?.InnerText ?? "",
                    Content = root["Content"]?.InnerText ?? "",
                    MessageId = root["MsgId"]?.InnerText ?? "",
                    EventKey = root["EventKey"]?.InnerText ?? "",
                };



                //异步任务完成通知
                if (model.EventName == "batch_job_result")
                {
                    model.BatchJob = new BatchJob()
                    {
                        JobId = root["BatchJob"]["JobId"]?.InnerText ?? "",
                        JobType = root["BatchJob"]["JobType"]?.InnerText ?? "",
                        ErrCode = root["BatchJob"]["ErrCode"]?.InnerText ?? "",
                        ErrMsg = root["BatchJob"]["ErrMsg"]?.InnerText ?? "",
                    };
                }

                //更新标签
                if (model.EventName == "change_contact" && model.ChangeType == "update_tag")
                {
                    var tagId = root["UpdateTag"]["TagId"]?.InnerText ?? "";
                    model.UpdateTag = new UpdateTag()
                    {
                        TagId = Convert.ToInt32(tagId),
                        AddPartyItems = (root["UpdateTag"]["AddPartyItems"]?.InnerText ?? "").Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList(),
                        DelPartyItems = (root["UpdateTag"]["DelPartyItems"]?.InnerText ?? "").Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList(),
                        AddUserItems = (root["UpdateTag"]["AddUserItems"]?.InnerText ?? "").Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList(),
                        DelUserItems = (root["UpdateTag"]["DelUserItems"]?.InnerText ?? "").Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList(),
                    };
                }


                //用户信息
                if (model.EventName == "change_contact" && model.ChangeType.EndsWith("te_user"))
                {
                    model.UpdateUser = new UpdateUser()
                    {
                        UserId = root["UserID"]?.InnerText ?? "",
                        NewUserId = root["NewUserId"]?.InnerText ?? "",
                        Department = (root["Department"]?.InnerText ?? "").Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x)).ToList(),
                    };
                }

                //扫码
                if(model.EventName == "scancode_push" || model.EventName == "scancode_waitmsg")
                {
                    model.ScanCodeInfo = new ScanCodeInfo()
                    {
                        ScanType = root["ScanCodeInfo"]?["ScanType"]?.InnerText ?? "",
                        ScanResult = root["ScanCodeInfo"]?["ScanResult"]?.InnerText ?? "",
                    };
                }

                return model;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public bool CheckSignature(Callback callback)
        {
            return CheckSignature(_options.Value.Token, callback);
        }

        /// <summary>
        /// 生成签名
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string GenerateSignature(Callback callback)
        {
            return GenerateSignature(_options.Value.Token, callback);
        }

        /// <summary>
        /// 解密企业微信的echostr
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public string DecryptEchostr(Callback callback)
        {
            return DecryptEchostr(_options.Value.EncodingAesKey, callback);
        }


        /// <summary>
        /// 解密企业微信的消息
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public string DecryptCallBackData(string payload)
        {
            return DecryptCallBackData(payload, _options.Value.EncodingAesKey, _options.Value.CorpID);
        }


        /// <summary>
        /// 加密企业微信回调数据
        /// </summary>
        /// <param name="payload">需要返回的内容</param>
        /// <returns></returns>
        public string EncryptCallBackData(string payload)
        {
            return EncryptCallBackData(payload, _options.Value.EncodingAesKey, _options.Value.CorpID);
        }


        /// <summary>
        /// 发送文本类型的回调消息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string SendTextMessage(string userId, string content)
        {
            return SendTextMessage(userId, content, _options.Value.CorpID);
        }
    }
}
