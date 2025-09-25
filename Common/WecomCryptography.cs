namespace DreamSlave.Wecom.Common
{
    internal class WecomCryptography
    {
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
        public static string AesDecrypt(String input, string encodingAESKey, out string corpid)
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
        public static String AesEncrypt(String input, string encodingAESKey, string corpid)
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
    }
}
