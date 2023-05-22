using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraManager
{
    public class HCameraBase
    {
        public MyCamera camera;

        public bool GetIntValue(string strKey, ref UInt32 pnValue)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = camera.MV_CC_GetIntValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.nCurValue;

            return true;
        }


        public bool GetIntMaxValue(string strKey, ref UInt32 pnValue)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = camera.MV_CC_GetIntValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.nMax;

            return true;
        }

        public bool GetIntMinValue(string strKey, ref UInt32 pnValue)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = camera.MV_CC_GetIntValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.nMin;

            return true;
        }

        public bool GetIntIntervalValue(string strKey, ref UInt32 pnValue)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = camera.MV_CC_GetIntValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.nInc;

            return true;
        }

        public bool SetIntValue(string strKey, UInt32 nValue)
        {


            int nRet = camera.MV_CC_SetIntValue_NET(strKey, nValue);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }

        public bool GetFloatValue(string strKey, ref float pfValue)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = camera.MV_CC_GetFloatValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pfValue = stParam.fCurValue;

            return true;
        }


        public bool GetFloatMaxValue(string strKey, ref float pnValue)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = camera.MV_CC_GetFloatValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.fMax;

            return true;
        }

        public bool GetFloatMinValue(string strKey, ref float pnValue)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = camera.MV_CC_GetFloatValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.fMin;

            return true;
        }

        public bool GetFloatIntervalValue(string strKey, ref float pnValue)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = camera.MV_CC_GetFloatValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = 1;

            return true;
        }

        public bool SetFloatValue(string strKey, float fValue)
        {
            int nRet = camera.MV_CC_SetFloatValue_NET(strKey, fValue);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }

        public bool GetEnumValue(string strKey, ref UInt32 pnValue)
        {
            MyCamera.MVCC_ENUMVALUE stParam = new MyCamera.MVCC_ENUMVALUE();
            int nRet = camera.MV_CC_GetEnumValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            pnValue = stParam.nCurValue;

            return true;
        }

        public bool SetEnumValue(string strKey, UInt32 nValue)
        {
            try
            {
                int nRet = camera.MV_CC_SetEnumValue_NET(strKey, nValue);
                if (MyCamera.MV_OK != nRet)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
           
        }

        public bool GetBoolValue(string strKey, ref bool pbValue)
        {
            int nRet = camera.MV_CC_GetBoolValue_NET(strKey, ref pbValue);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            return true;
        }

        public bool SetBoolValue(string strKey, bool bValue)
        {
            int nRet = camera.MV_CC_SetBoolValue_NET(strKey, bValue);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }

        public bool GetStringValue(string strKey, ref string strValue)
        {
            MyCamera.MVCC_STRINGVALUE stParam = new MyCamera.MVCC_STRINGVALUE();
            int nRet = camera.MV_CC_GetStringValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            strValue = stParam.chCurValue;

            return true;
        }

        public bool SetStringValue(string strKey, string strValue)
        {
            int nRet = camera.MV_CC_SetStringValue_NET(strKey, strValue);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }

        public bool CommandExecute(string strKey)
        {
            int nRet = camera.MV_CC_SetCommandValue_NET(strKey);
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }
    }
}
