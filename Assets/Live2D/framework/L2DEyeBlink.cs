/**
 *
 *  You can modify and use this source freely
 *  only for the development of application related Live2D.
 *
 *  (c) Live2D Inc. All rights reserved.
 */
using System.Collections;
using live2d;

namespace live2d.framework
{
    /*
     * 눈 깜빡임 모션
     * Live2D 라이브러리 EyeBlinkMotion 클래스와 거의 동일
     * 어느쪽을 사용해도 좋지만 확장하려면 이걸 쓴다.
     */

    // 눈 상태 상수
    enum EYE_STATE
    {
        STATE_FIRST,
        STATE_INTERVAL,
        STATE_CLOSING,// 감는 도중
        STATE_CLOSED,// 감은 상태
        STATE_OPENING,// 뜨는 도중
    }
    public class L2DEyeBlink
    {
        // ---- 내부 데이터 ----
        private bool _enabled;
        long nextBlinkTime;// 다음에 깜빡이는 시간(msec)
        long stateStartTime;// 현재 상태가 시작된 시간

        EYE_STATE eyeState;// 현재 상태

        bool closeIfZero;// ID로 지정된 눈의 매개 변수가 0 일 때 감는다면 true, 1 일 때 감는 경우 false

        string eyeID_L;
        string eyeID_R;
        // ------------ 설정 ------------
        int blinkIntervalMsec;

        int closingMotionMsec;// 눈이 감길 때 까지의 시간
        int closedMotionMsec;// 감은 채 있는 시간
        int openingMotionMsec;// 눈을 뜰 때 까지의 시간


        public L2DEyeBlink()
        {
            _enabled = false;
            eyeState = EYE_STATE.STATE_FIRST;

            blinkIntervalMsec = 4000;

            closingMotionMsec = 100;// 眼が閉じるまでの時間
            closedMotionMsec = 50;// 閉じたままでいる時間
            openingMotionMsec = 150;// 眼が開くまでの時間

            closeIfZero = true;// IDで指定された眼のパラメータが、0のときに閉じるなら true 、1の時に閉じるなら false

            eyeID_L = "PARAM_EYE_L_OPEN";
            eyeID_R = "PARAM_EYE_R_OPEN";
        }

        public void SetEnabled(bool enabled)
        {
            if (_enabled != enabled)
            {
                if (enabled)
                {
                    eyeState = EYE_STATE.STATE_FIRST;
                }
                _enabled = enabled;
            }
        }

        /*
         * 다음 번 눈 깜빡거릴 시간 결정
         * @return
         */
        public long calcNextBlink()
        {
            long time = UtSystem.getUserTimeMSec();
            double r = UtMath.randDouble();//0..1
            return (long)(time + r * (2 * blinkIntervalMsec - 1));
        }


        public void setInterval(int blinkIntervalMsec)
        {
            this.blinkIntervalMsec = blinkIntervalMsec;
        }


        public void setEyeMotion(int closingMotionMsec, int closedMotionMsec, int openingMotionMsec)
        {
            this.closingMotionMsec = closingMotionMsec;
            this.closedMotionMsec = closedMotionMsec;
            this.openingMotionMsec = openingMotionMsec;
        }


        /*
         * 모델의 파라미터 갱신
         * @param model
         */
        public void updateParam(ALive2DModel model)
        {
            if (!_enabled)
            {
                return;
            }

            long time = UtSystem.getUserTimeMSec();
            float eyeParamValue;// 설정할 값
            float t = 0;

            switch (this.eyeState)
            {
                case EYE_STATE.STATE_CLOSING:
                    // 閉じるまでの割合を0..1に直す(blinkMotionMsecの半分の時間で閉じる)
                    t = (time - stateStartTime) / (float)closingMotionMsec;
                    if (t >= 1)
                    {
                        t = 1;
                        this.eyeState = EYE_STATE.STATE_CLOSED;// 次から開き始める
                        this.stateStartTime = time;
                    }
                    eyeParamValue = 1 - t;
                    break;
                case EYE_STATE.STATE_CLOSED:
                    t = (time - stateStartTime) / (float)closedMotionMsec;
                    if (t >= 1)
                    {
                        this.eyeState = EYE_STATE.STATE_OPENING;// 次から開き始める
                        this.stateStartTime = time;
                    }
                    eyeParamValue = 0;// 閉じた状態
                    break;
                case EYE_STATE.STATE_OPENING:
                    t = (time - stateStartTime) / (float)openingMotionMsec;
                    if (t >= 1)
                    {
                        t = 1;
                        this.eyeState = EYE_STATE.STATE_INTERVAL;// 次から開き始める
                        this.nextBlinkTime = calcNextBlink();// 次回のまばたきのタイミングを始める時刻
                    }
                    eyeParamValue = t;
                    break;
                case EYE_STATE.STATE_INTERVAL:
                    //
                    if (this.nextBlinkTime < time)
                    {
                        this.eyeState = EYE_STATE.STATE_CLOSING;
                        this.stateStartTime = time;
                    }
                    eyeParamValue = 1;// 開いた状態
                    break;
                case EYE_STATE.STATE_FIRST:
                default:
                    this.eyeState = EYE_STATE.STATE_INTERVAL;
                    this.nextBlinkTime = calcNextBlink();// 次回のまばたきのタイミングを始める時刻
                    eyeParamValue = 1;// 開いた状態
                    break;
            }

            if (!closeIfZero) eyeParamValue = -eyeParamValue;

            // ---- 値を設定 ----
            model.setParamFloat(eyeID_L, eyeParamValue);
            model.setParamFloat(eyeID_R, eyeParamValue);
        }
    }
}