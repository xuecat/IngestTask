using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace IngestTask.Tool
{
    public class CTimeCode
    {
        public static long BASED_SECOND { get; set; } = 25L;
        public static long BASED_MINUTE { get; set; } = (60L*BASED_SECOND);
        public static long BASED_HOUR { get; set; } = (60L*BASED_MINUTE);

        public static long NTSC30_SECOND { get; set; } = 30L;
        public static long NTSC30_MINUTE { get; set; } = (60L*NTSC30_SECOND);
        public static long NTSC30_HOUR { get; set; } = (60L*NTSC30_MINUTE);

        public static long NTSC_HOUR { get; set; } = 107892;
        public static long NTSC_TENMIN { get; set; } = 17982;
        public static long NTSC_MIN { get; set; } = 1798;

        //以下定义720P的参数
        public static long P_50P_SECOND { get; set; } = 50L;
        public static long P_50P_MINUTE { get; set; } = (60L*P_50P_SECOND);
        public static long P_50P_HOUR { get; set; } = (60L*P_50P_MINUTE);

        public static long P_5994P_SECOND { get; set; } = 60L;
        public static long P_5994P_MIN { get; set; } = 3596;
        public static long P_5994P_TENMIN { get; set; } = 35964;
        public static long P_5994P_HOUR { get; set; } = 215784;

        public static long P_60P_SECOND { get; set; } = 60L;
        public static long P_60P_MIN { get; set; } = (60L*P_60P_SECOND);
        public static long P_60P_HOUR { get; set; } = (60L*P_60P_MIN);

        //统一系统制式的定义到ML的定义，避免计算错误
        //视频模式
        public enum VideoStandard
        {
	        e_VS_PAL = 1,
	        e_VS_NTSC2997 = 2,
	        e_VS_PAL_50P = 3,
	        e_VS_NTSC5994P = 4,
	        e_VS_NTSC30 = 99,//这个ML这边现在已经没有了，暂时改成一个没有的数字，不用
        };
        public enum DFMode
        {
            e_DF_Yes = 1,
            e_DF_No = 0
        };

        public enum PShowMode
        {
            e_SM_Half = 0,
            e_SM_Normal = 1
        };
        public string m_timecode { get; set; }

        Int16 m_hour;
        Int16 m_minute;
        Int16 m_second;
        Int16 m_frame;
        int m_totalFrame;
        int m_maxFrame;
        int m_minFrame;
        public VideoStandard VS { get; set; }
        public double dbFramerate { get; set; }
        public DFMode m_dfMode { get; set; }
        public PShowMode m_PSMMode { get; set; }    // 720P的显示模式


        public CTimeCode()
        {
            Clear();
            m_dfMode = DFMode.e_DF_Yes;
            m_PSMMode = PShowMode.e_SM_Half;
        }

        public void setDBFrameRate(double dbFrame)
        {
            dbFramerate = dbFrame;
        }
        public CTimeCode(uint dwFrm)
        {
            SetCode(dwFrm);
        }

        bool SetCode(long dwFrm)
        {
            if(VS == VideoStandard.e_VS_PAL_50P)
            {
                m_hour = (short)(dwFrm / P_50P_HOUR);
                m_minute = (short)((dwFrm % P_50P_HOUR) / P_50P_MINUTE);
                m_second = (short)((dwFrm % P_50P_MINUTE) / P_50P_SECOND);
                m_frame = (short)(dwFrm % P_50P_SECOND);
            }
            else if(VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes)
            {
                m_hour = (short)(dwFrm / P_5994P_HOUR);
                int nFreeFrame = (int)(dwFrm % P_5994P_HOUR);

                int nTenMin = (int)(nFreeFrame / P_5994P_TENMIN);
                nFreeFrame = (int)(nFreeFrame % P_5994P_TENMIN);

                int nMin = (int)((nFreeFrame - 4) / P_5994P_MIN);
                m_minute = (short)(nMin + nTenMin * 10);
                nFreeFrame = (int)(nFreeFrame - nMin * P_5994P_MIN);

                m_second = (short)(nFreeFrame / P_5994P_SECOND);
                m_frame = (short)(nFreeFrame / P_5994P_SECOND);
            }
            else if((VS == VideoStandard.e_VS_NTSC5994P) && (m_dfMode == DFMode.e_DF_No))
            {
                m_hour = (short)(dwFrm / P_60P_HOUR);
                m_minute = (short)((dwFrm % P_60P_HOUR) / P_60P_MIN);
                m_second = (short)((dwFrm % P_60P_MIN) / P_60P_SECOND);
                m_frame = (short)((dwFrm % P_60P_SECOND));
            }
            else if((VS == VideoStandard.e_VS_NTSC2997) && (m_dfMode == DFMode.e_DF_Yes))
            {
                long nFreeFrame = (int)(dwFrm % NTSC_HOUR);

                m_hour = (short)(dwFrm / NTSC_HOUR);
                int nTenMin = (int)(nFreeFrame / NTSC_TENMIN);
                nFreeFrame = (int)(nFreeFrame % NTSC_TENMIN);

                long nMin = (nFreeFrame - 2) / NTSC_MIN;
                m_minute = (short)(nMin + nTenMin * 10);
                nFreeFrame = nFreeFrame - nMin * NTSC_MIN;

                m_second = (short)(nFreeFrame / 30);
                m_frame = (short)(nFreeFrame % 30);
            }
            else if((VS == VideoStandard.e_VS_NTSC30) || (VS == VideoStandard.e_VS_NTSC2997)
                && m_dfMode == (DFMode.e_DF_No))
            {
                m_hour = (short)(dwFrm / NTSC30_HOUR);
                m_minute = (short)((dwFrm % NTSC30_HOUR) / NTSC30_MINUTE);
                m_second = (short)((dwFrm % NTSC30_MINUTE) / NTSC30_SECOND);
                m_frame = (short)((dwFrm % NTSC30_SECOND));
            }
            else if(VS == VideoStandard.e_VS_PAL)
            {
                m_hour = (short)(dwFrm / BASED_HOUR);
                m_minute = (short)((dwFrm % BASED_HOUR) / BASED_MINUTE);
                m_second = (short)((dwFrm % BASED_MINUTE) / BASED_SECOND);
                m_frame = (short)((dwFrm % BASED_SECOND));
            }

            return true;
        }

        public VideoStandard Rate2VideoStandard(float rateframe)
        {
            switch (rateframe)
            {
               
                case 25.0f:
                    return VideoStandard.e_VS_PAL;
                case 29.97f:
                    return VideoStandard.e_VS_NTSC2997;
                case 30.0f:
                    return VideoStandard.e_VS_NTSC30;
                case 50.0f:
                    return VideoStandard.e_VS_PAL_50P;
                case 59.94f:
                    return VideoStandard.e_VS_NTSC5994P;
                case 23.97f:
                case 24.0f:
                case 60.0f:
                default://25.0f
                    return VideoStandard.e_VS_PAL;
            }
        }
        public long GetFrameByTimeCode(long dwHmsf)
        {
            
            if (dwHmsf >= 0)
            {
                long hour = ((char)(dwHmsf >> 24));
                long min = ((char)((dwHmsf >> 16) & 0x000000ff));
                long second = ((char)((dwHmsf >> 8) & 0x000000ff));
                long frame = ((char)(dwHmsf & 0x000000ff));

               SetCode2(hour, min, second, frame);

                return GetRealFrames();
            }

            return 0;
        }
        public void GetCode(ref string str)
        {
            if ((VS == VideoStandard.e_VS_NTSC2997) && (m_dfMode == DFMode.e_DF_Yes))
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame);
            }
            else if ((VS == VideoStandard.e_VS_PAL_50P) ||
                ((VS == VideoStandard.e_VS_NTSC5994P) && (m_dfMode == DFMode.e_DF_No)))
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame * ((int)m_PSMMode + 1) / 2);
            }
            else if((VS == VideoStandard.e_VS_NTSC5994P) && (m_dfMode == DFMode.e_DF_Yes))
            {
                str = string.Format("{0}{1}{2}{3}",
                    m_hour, m_minute, m_second, m_frame*((int)(m_PSMMode) + 1) / 2);

            }
            else
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame);
            }
        }

        public string GetCode()
        {
            string str = "";

            if ((VS == VideoStandard.e_VS_NTSC2997) && (m_dfMode == DFMode.e_DF_Yes))
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame);
            }
            else if ((VS == VideoStandard.e_VS_PAL_50P) ||
                ((VS == VideoStandard.e_VS_NTSC5994P) && (m_dfMode == DFMode.e_DF_No)))
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame * ((int)m_PSMMode + 1) / 2);
            }
            else if ((VS == VideoStandard.e_VS_NTSC5994P) && (m_dfMode == DFMode.e_DF_Yes))
            {
                str = string.Format("{0}{1}{2}{3}",
                    m_hour, m_minute, m_second, m_frame * ((int)(m_PSMMode) + 1) / 2);

            }
            else
            {
                str = string.Format("{0}{1}{2}{3}", m_hour, m_minute, m_second, m_frame);
            }

            return str;
        }

        public void GetCode2(ref long h, ref long m, ref long s, ref long f)
        {
            h = m_hour;
            m = m_minute;
            s = m_second;
            f = m_frame;

            if(VS == VideoStandard.e_VS_PAL_50P || VS == VideoStandard.e_VS_NTSC5994P)
            {
                f = (m_frame) * ((int)m_PSMMode + 1) / 2;
            }
        }

        public void GetCode(ref long h, ref long m, ref long s, ref long f)
        {
            h = m_hour;
            m = m_minute;
            s = m_second;
            f = m_frame;
        }

        public bool SetCode2(long h, long m, long s, long f)
        {
            int fps = 25;
            if (VS == VideoStandard.e_VS_PAL)
            {
                fps = 25;
            }
            else if(VS == VideoStandard.e_VS_NTSC2997 || VS == VideoStandard.e_VS_NTSC30)
            {
                fps = 30;
            }
            else if(VS == VideoStandard.e_VS_PAL_50P)
            {
                fps = 50;
                f = (f * 2 / ((int)m_PSMMode + 1)) % fps;
            }
            else if(VS == VideoStandard.e_VS_NTSC5994P)
            {
                fps = 60;
                f = (f * 2 / ((int)m_PSMMode + 1)) % fps;
            }

            // 处理clock工作模式时可能传入的非法时码
            if((VS == VideoStandard.e_VS_NTSC2997) && m_dfMode == DFMode.e_DF_Yes
                && m % 10 != 0 && s == 0 && f < 2)
            {
                f = 2;
            }

            if((VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes)
                && m % 10 != 0 && s == 0 && f < 4)
            {
                f = 4;  // 5994i每分钟丢4帧
            }

            if (h > 59 || m > 59 || f > (fps - 1))
                return false;

            m_hour = (short)h;
            m_minute = (short)m;
            m_second = (short)s;
            m_frame = (short)f;

            return true;
        }
        //
        // 摘要：
        //      添加一个设置时码的接口，方便外部调用
        //
        public bool SetCode(long h, long m, long s, long f)
        {
            int fps = 25;
            if(VS == VideoStandard.e_VS_PAL)
            {
                fps = 25;
            }
            else if(VS == VideoStandard.e_VS_NTSC2997 || VS == VideoStandard.e_VS_NTSC30)
            {
                fps = 30;
            }
            else if(VS == VideoStandard.e_VS_PAL_50P)
            {
                fps = 50;
            }
            else if(VS == VideoStandard.e_VS_NTSC5994P)
            {
                fps = 60;
            }

            // 处理clock工作模式时可能传入的非法时码
            if(VS == VideoStandard.e_VS_NTSC2997 && m_dfMode == DFMode.e_DF_Yes
                && m % 10 != 0 && s == 0 && f < 2)
            {
                f = 2;
            }

            if((VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes)
                && m % 10 != 0 && s == 0 && f < 4)
            {
                // 5994i每分钟丢一帧
                f = 4;
            }

            if (h > 59 || m > 59 || f > (fps - 1))
                return false;

            m_hour = (short)h;
            m_minute = (short)m;
            m_second = (short)s;
            m_frame = (short)f;
            return true;
        }

        //
        // 摘要：
        //      50P的制式，传进来的帧是指显示帧，比如00：00：00：02表示的是4
        //
        public bool SetCode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            string str_hour = "0", str_minute = "0", str_second = "0", str_frame = "0";
            if (str.Length > 2)
            {
                str_hour = string.Format("{0}{1}", str[0], str[1]);
            }
            if (str.Length > 4)
            {
                str_minute = string.Format("{0}{1}", str[3], str[4]);
            }
            if (str.Length > 6)
            {
                str_second = string.Format("{0}{1}", str[6], str[7]);
            }

            if (str.Length > 8)
            {
                str_frame = string.Format("{0}{1}", str[9], str[10]);
            }

            int h = Convert.ToInt32(str_hour);
            int m = Convert.ToInt32(str_minute);
            int s = Convert.ToInt32(str_second);
            int f = Convert.ToInt32(str_frame);

            return SetCode2(h, m, s, f);
        }

        public bool SetCode2(string str)
        {
            string str_hour = "0", str_minute = "0", str_second = "0", str_frame="0";
            str_hour = string.Format("{0}{1}", str[0], str[1]);
            str_minute = string.Format("{0}{1}", str[3], str[4]);
            str_second = string.Format("{0}{1}", str[6], str[7]);
            if (str.Length == 11)
            {
                str_frame = string.Format("{0}{1}", str[9], str[10]);
            }
            else if (str.Length == 12)
                str_frame = string.Format("{0}{1}{2}", str[9], str[10], str[11]);

            int h = Convert.ToInt32(str_hour);
            int m = Convert.ToInt32(str_minute);
            int s = Convert.ToInt32(str_second);
            int f = Convert.ToInt32(str_frame);

            return SetCode2(h, m, s, f);
        }

        //
        // 摘要：
        //      HY:此处应支持DF/NDF模式
        //  
        /*
       HY:此处应支持DF/NDF模式
       */
        /*
        ychuang20090109
        修改内容：添加DF/NDF模式的判断支持
        */
        public Int64 GetStampFrames()
        {
            if (VS == VideoStandard.e_VS_PAL)
                return (m_frame + m_second * 25 + m_minute * 25 * 60 + m_hour * 25 * 3600);
            else if (VS == VideoStandard.e_VS_PAL_50P)
                return (m_frame + m_second * P_50P_SECOND + m_minute * P_50P_MINUTE + m_hour * P_50P_HOUR);
            else if (VS == VideoStandard.e_VS_NTSC30)
                return (m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
            else if (VS == VideoStandard.e_VS_NTSC2997)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    return (Int64)(NTSC_HOUR * m_hour + NTSC_TENMIN * (m_minute / 10) + NTSC_MIN * (m_minute % 10) + m_second * 30 + m_frame);
                }
                else if (m_dfMode == DFMode.e_DF_No)
                {
                    return (m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
                }
            }
            else if (VS == VideoStandard.e_VS_NTSC5994P)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    return (Int64)(P_5994P_HOUR * m_hour + P_5994P_TENMIN * (m_minute / 10) + P_5994P_MIN * (m_minute % 10) + m_second * P_5994P_SECOND + m_frame);
                }
                else
                {
                    return (Int64)(m_frame + m_second * P_60P_SECOND + m_minute * P_60P_MIN + m_hour * P_60P_HOUR);
                }
            }

            return 0;
        }

        public uint GetRealFrames()
        {
            if (VS == VideoStandard.e_VS_PAL)
            {
                return (uint)(m_frame + m_second * 25 + m_minute * 25 * 60 + m_hour * 25 * 3600);
            }
            else if (VS == VideoStandard.e_VS_PAL_50P)
            {
                return (uint)(m_frame + m_second * 50L + m_minute * 50L * 60L + m_hour * 50L * 3600);
            }
            else if (VS == VideoStandard.e_VS_NTSC30)
            {
                return (uint)(m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
            }
            else if (VS == VideoStandard.e_VS_NTSC2997)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    return (uint)(NTSC_HOUR * m_hour + NTSC_TENMIN * (m_minute / 10) + NTSC_MIN * (m_minute % 10) + m_second * 30 + m_frame);
                }
                else
                    return (uint)(m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
            }
            else if (VS == VideoStandard.e_VS_NTSC5994P)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    return (uint)(P_5994P_HOUR * m_hour + P_5994P_TENMIN * (m_minute / 10) + P_5994P_MIN * (m_minute % 10) + m_second * P_5994P_SECOND + m_frame);
                }
                else
                    return (uint)(m_frame + m_second * P_60P_SECOND + m_minute * P_60P_MIN + m_hour * P_60P_HOUR);
            }

            Double dFrameRate = dbFramerate;
            if (m_hour > 99 || m_minute > 59 || m_second > 59 || m_frame >= (int)(Math.Ceiling(dFrameRate)))
                return 0;

            int iMul = 1;
            if (dFrameRate > 59 && dFrameRate < 59.95) { iMul = 2; dFrameRate = 29.97; }
            else if (50 == dFrameRate) { /*iMul = 2; dFrameRate = 25;*/ m_frame = (short)(m_frame / 2); }
            else if (60 == dFrameRate) { iMul = 2; dFrameRate = 30; }
            
            if (m_dfMode == DFMode.e_DF_No)
            {
                dFrameRate = 30;
            }
            if (dFrameRate > 29 && dFrameRate < 30)
            {
                if ((m_minute % 10) != 0)
                {
                    if (m_second == 0 && (m_frame == 0 || m_frame == 1))
                        m_frame = 2;
                }
            }
            return (uint)((Math.Round(3600 * dFrameRate) * m_hour + Math.Round(600 * dFrameRate) * (m_minute / 10) + Math.Round(60 * dFrameRate) * (m_minute % 10) + m_second * Math.Ceiling(dFrameRate) + m_frame) * iMul);

        }
        public uint GetFrames()
        {
            Double dFrameRate = dbFramerate;
            //GetMSVFrameRate(ref dFrameRate);
            //m_data.GetMSVFrameRate(ref nFramerate);
            //double dbFramerate = BuilderTool.GetFrameRateFloat((emMSVFrameRate)nFramerate);

            if (m_hour > 99 || m_minute > 59 || m_second > 59 || m_frame >= (int)(Math.Ceiling(dFrameRate)))
                return 0;

            int iMul = 1;
            if (dFrameRate > 59 && dFrameRate < 59.95) { iMul = 2; dFrameRate = 29.97; }
            else if (50 == dFrameRate) { iMul = 2; dFrameRate = 25; }
            else if (60 == dFrameRate) { iMul = 2; dFrameRate = 30; }

            if(m_dfMode == DFMode.e_DF_No)
            {
                dFrameRate = 30;
            }
            if (dFrameRate > 29 && dFrameRate < 30)
            {
                if ((m_minute % 10) != 0)
                {
                    if (m_second == 0 && (m_frame == 0 || m_frame == 1))
                        m_frame = 2;
                }
            }
            return (uint)((Math.Round(3600 * dFrameRate) * m_hour + Math.Round(600 * dFrameRate) * (m_minute / 10) + Math.Round(60 * dFrameRate) * (m_minute % 10) + m_second * Math.Ceiling(dFrameRate) + m_frame) * iMul); 

            //if (VS == VideoStandard.e_VS_PAL)
            //{
            //    return (uint)((uint)m_frame + m_second * 25 + m_minute * 25 * 60 + m_hour * 25 * 3600);
            //}
            //else if(VS == VideoStandard.e_VS_PAL_50P)
            //{
            //    return (uint)((uint)m_frame + m_second * P_50P_SECOND + m_minute * P_50P_MINUTE
            //        + m_hour * P_50P_HOUR);
            //}
            //else if(VS == VideoStandard.e_VS_NTSC30)
            //{
            //    return (uint)((uint)m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
            //}
            //else if(VS == VideoStandard.e_VS_NTSC2997)
            //{
            //    if(m_dfMode == DFMode.e_DF_Yes)
            //    {
            //        return (uint)((uint)m_hour * NTSC_HOUR + (m_minute / 10) * NTSC_TENMIN +
            //            NTSC_MIN * (m_minute % 10) + m_second * 30 + m_frame);
            //    }
            //    else if(m_dfMode == DFMode.e_DF_No)
            //    {
            //        return (uint)((uint)m_frame + m_second * 30 + m_minute * 30 * 60 + m_hour * 30 * 3600);
            //    }
            //}
            //else if(VS == VideoStandard.e_VS_NTSC5994P)
            //{
            //    if(m_dfMode == DFMode.e_DF_Yes)
            //    {
            //        return (uint)((uint)m_hour * P_5994P_HOUR + P_5994P_TENMIN * (m_minute / 10) +
            //            P_5994P_MIN * (m_minute % 10) + m_second * P_5994P_SECOND + m_frame);
            //    }
            //    else
            //    {
            //        return (uint)((uint)m_frame + m_second * P_60P_SECOND + m_minute * P_60P_MIN +
            //            m_hour * P_60P_HOUR);
            //    }
            //}

            //return 0;
        }

        public static CTimeCode operator-(CTimeCode lhs, int frame)
        {
            int dw;
            dw = (int)(lhs.GetFrames() - frame);
            if(dw < lhs.m_minFrame)
            {
                dw += lhs.m_maxFrame;
            }
            else if(dw >= lhs.m_maxFrame)
            {
                dw -= lhs.m_maxFrame;
            }

            lhs.SetCode((uint)dw);
            return lhs;
        }

        public static CTimeCode operator+(CTimeCode lhs, int frame)
        {
            int dw;
            dw = (int)(lhs.GetFrames() + frame);
            if (dw < lhs.m_minFrame)
                dw += lhs.m_maxFrame;
            else if (dw >= lhs.m_maxFrame)
                dw -= lhs.m_maxFrame;

            lhs.SetCode((uint)dw);

            return lhs;
        }

        public void AddFrame(int frame)
        {
            int nFrame = 0;
            int nTemp = 0;
            int nFps = 25;

            if (VS == VideoStandard.e_VS_NTSC30 || VS == VideoStandard.e_VS_NTSC2997)
                nFps = 30;
            else if (VS == VideoStandard.e_VS_PAL_50P)
                nFps = 50;
            else if (VS == VideoStandard.e_VS_NTSC5994P)
                nFps = 60;

            switch(frame)
            {
                case 0:
                    nTemp = m_hour + 1;
                    break;

                case 1:
                    nTemp = m_minute + 1;
                    break;

                case 2:
                    nTemp = m_second + 1;
                    break;

                case 3:
                    nTemp = m_frame + 1;
                    break;

                default:
                    break;
            }

            if((frame < 3 && nTemp > 59 && frame != 0) || 
                (frame == 3 && nTemp > (nFps - 1)) ||
                (frame == 0 && nTemp > 99))
            {
                switch(frame)
                {
                    case 0: nFrame = GetBaseDegree(0, 0, 0, 0); break;
                    case 1: nFrame = GetBaseDegree(0, 1, 0, 0); break;
                    case 2: nFrame = GetBaseDegree(0, 0, 1, 0); break;
                    case 3: nFrame = GetBaseDegree(0, 0, 0, 1); break;
                    default: break;
                }

                int tempFrame = (int)(GetFrames() + nFrame);
                if (tempFrame > m_maxFrame || tempFrame < m_minute)
                    return;

                m_totalFrame = tempFrame;
                if (VS == VideoStandard.e_VS_NTSC2997 && m_dfMode == DFMode.e_DF_Yes && frame == 2 && m_frame > 1)
                    m_totalFrame -= 2;
                if (VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes && frame == 2 && m_frame > 3)
                    m_totalFrame -= 4;
            }
            else
            {
                short timeBuf = 0;
                switch(frame)
                {
                    case 0: timeBuf = m_hour; m_hour = (short)nTemp; break;
                    case 1: timeBuf = m_minute; m_minute = (short)nTemp; break;
                    case 2: timeBuf = m_second; m_second = (short)nTemp; break;
                    case 3: timeBuf = m_frame; m_frame = (short)nTemp; break;
                    default: break;
                }
                if (VS == VideoStandard.e_VS_NTSC2997 && m_dfMode == DFMode.e_DF_Yes)
                    if (m_minute % 10 != 0 && m_second == 0 && m_frame < 2)
                        m_frame = 2;
                if (VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes)
                    if (m_minute % 10 != 0 && m_second == 0 && m_frame < 4)
                        m_frame = 4;

                uint buf = (uint)GetBaseDegree(m_hour, m_minute, m_second, m_frame);

                if(buf > m_maxFrame)
                {
                    switch(frame)
                    {
                        case 0: m_hour = (short)timeBuf; break;
                        case 1: m_minute = (short)timeBuf; break;
                        case 2: m_second = (short)timeBuf; break;
                        case 3: m_frame = (short)timeBuf; break;
                        default: break;

                    }

                    return;
                }

                m_totalFrame = (int)buf;
            }

            SetCode((uint)m_totalFrame);
        }

        public void SubFrame(int frame)
        {
            int nFrame = 0;
            switch(frame)
            {
                case 0: nFrame = GetBaseDegree(1, 0, 0, 0); break;
                case 1: nFrame = GetBaseDegree(0, 1, 0, 0); break;
                case 2: nFrame = GetBaseDegree(0, 0, 1, 0); break;
                case 3: nFrame = GetBaseDegree(0, 0, 0, 1); break;
                default: break;
            }

            int nTemp = (int)(GetFrames() - nFrame);
            if (nTemp < 0 || nTemp < m_minFrame) return;
            m_totalFrame = nTemp;
            if(VS == VideoStandard.e_VS_NTSC2997 && 
                m_dfMode == DFMode.e_DF_Yes && m_minute % 10 != 0
                && (frame == 1 || (frame == 2 && m_second == 0)))
            {
                m_totalFrame += 2;
            }

            if(VS == VideoStandard.e_VS_NTSC5994P && m_dfMode == DFMode.e_DF_Yes
                && m_minute % 10 != 0 
                && (frame == 1 || frame == 2 && m_second == 0))
            {
                m_totalFrame += 4;
            }

            SetCode((uint)m_totalFrame);
        }

        public int GetBaseDegree(int h, int m, int s, int f)
        {
            if (VS == VideoStandard.e_VS_PAL)
            {
                return (f + s * 25 + m * 25 * 60 + h * 25 * 3600);
            }
            else if (VS == VideoStandard.e_VS_PAL_50P)
            {
                return (int)(f + s * P_50P_SECOND + m * P_50P_MINUTE + h * P_50P_HOUR);
            }
            else if (VS == VideoStandard.e_VS_NTSC30)
                return (int)(f + s * 30 + m * 30 * 60 + h * 30 * 3600);
            else if(VS == VideoStandard.e_VS_NTSC2997)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    if (m % 10 != 0 && s == 0 && f < 2)
                        f = 2;
                    return (int)(h * NTSC_HOUR + NTSC_TENMIN * (m / 10) +
                        NTSC_MIN * (m % 10) + s * 30 + f);
                }
                else
                    return (f + s * 30 + m * 30 * 60 + h * 30 * 3600);
            }
            else if(VS == VideoStandard.e_VS_NTSC5994P)
            {
                if (m_dfMode == DFMode.e_DF_Yes)
                {
                    if (m % 10 != 0 && s == 0 && f < 4)
                        f = 4;
                    return (int)(h * P_5994P_HOUR + (m / 10) * P_5994P_TENMIN +
                        P_5994P_MIN * (m % 10) + s * P_5994P_SECOND + f);
                }
                else
                    return (int)(f + s * P_60P_SECOND + m * P_60P_MIN + h * P_60P_HOUR);
            }

            return 0;
        }

        public void Clear()
        {
            m_hour = 0;
            m_minute = 0;
            m_second = 0;
            m_frame = 0;
            m_totalFrame = 0;
            SetVS(VideoStandard.e_VS_PAL);
        }

        public void SetVS(VideoStandard nVs)
        {
            VS = nVs;

            // 根据制式，设定最大值，因为是时码的计算，所以当帧数为负数或者大于最大值的时候，自动按照最大值补齐
            m_minFrame = 0;

            switch(VS)
            {
                case VideoStandard.e_VS_PAL:
                    m_maxFrame = 24 * 25 * 3600 - 1; // 24小时
                    break;
                case VideoStandard.e_VS_PAL_50P:
                    m_maxFrame = (int)(P_50P_HOUR * 24 - 1);
                    break;
                case VideoStandard.e_VS_NTSC30:
                    m_maxFrame = 24 * 30 * 3600 - 1;
                    break;
                case VideoStandard.e_VS_NTSC2997:
                    {
                        if(m_dfMode == DFMode.e_DF_Yes)
                        {
                            m_maxFrame = (int)(NTSC_HOUR * 24 - 1);
                        }
                        else if(m_dfMode == DFMode.e_DF_No)
                        {
                            m_maxFrame = 24 * 3600 * 30 - 1;
                        }
                    }
                    break;
                case VideoStandard.e_VS_NTSC5994P:
                    {
                        if(DFMode.e_DF_Yes == m_dfMode)
                        {
                            m_maxFrame = (int)(P_5994P_HOUR * 24 - 1);
                        }
                        else
                        {
                            m_maxFrame = (int)(P_60P_HOUR * 24 - 1);
                        }
                    }
                    break;
                default: break;
            }
        }

        public void SetDFMode(int nDF)
        {
            m_dfMode = (DFMode)nDF;
        }

        public void SetShowMode(uint uSMMode)
        {
            m_PSMMode = (PShowMode)uSMMode;
        }
    }
}
