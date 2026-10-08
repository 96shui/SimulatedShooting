using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Common;

namespace VRShooting.Unity.UI
{
    public interface IDroneReconVideoSource { Texture ResolveVideo(string bindingId); }

    /// <summary>BDD28 presentation only: no scene progression, damage or combat commands.</summary>
    public sealed class P3DroneReconView : MonoBehaviour
    {
        [SerializeField,Range(0,1)] float saturation=.18f;
        [SerializeField] Color monitorTint=new Color(.78f,.88f,.78f,1);
        [SerializeField,Range(0,.15f)] float noise=.012f;
        [SerializeField,Range(0,.3f)] float scanlines=.035f;
        [SerializeField,Range(0,.5f)] float vignette=.16f;
        RawImage feed;
        GameObject monitor,reticle;
        TMP_Text phase,status,elapsed,telemetry,error;
        Button back,retry;
        Material filter;
        public event Action BackRequested;
        public event Action RetryRequested;
        public DroneReconSnapshotDto LastSnapshot {get;private set;}
        public RawImage Feed=>feed;
        public void Configure(RawImage image,GameObject videoPanel,GameObject frame,
            TMP_Text phaseLabel,TMP_Text statusLabel,TMP_Text elapsedLabel,TMP_Text telemetryLabel,
            TMP_Text errorLabel,Button backButton,Button retryButton)
        {
            ReleaseBindings();
            feed=image;monitor=videoPanel;reticle=frame;phase=phaseLabel;status=statusLabel;
            elapsed=elapsedLabel;telemetry=telemetryLabel;error=errorLabel;back=backButton;retry=retryButton;
            back.onClick.AddListener(Back);retry.onClick.AddListener(Retry);
            var shader=Resources.Load<Shader>("UI/DroneMonitor");
            if(shader!=null){filter=new Material(shader){name="Mode3_MonitorFilter"};feed.material=filter;}
        }
        void Back()=>BackRequested?.Invoke();
        void Retry()=>RetryRequested?.Invoke();
        public void Apply(DroneReconSnapshotDto value,Texture texture,bool busy)
        {
            LastSnapshot=value;
            var observing=value.ViewMode==DroneReconViewMode.DroneFeed;
            monitor.SetActive(observing);reticle.SetActive(observing);
            feed.texture=value.FeedAvailable?texture:null;
            phase.text=PhaseLabel(value.Phase);
            status.text=value.FeedAvailable&&texture!=null?"LIVE · 实时视频":"视频未就绪";
            elapsed.text="侦察用时 "+Mathf.Max(0,value.ReconElapsedSeconds).ToString("F1")+" s";
            telemetry.text=value.TelemetryValid
                ? "高度 "+value.HeightAboveGroundMeters.ToString("F1")+" m   速度 "+value.SpeedMetersPerSecond.ToString("F1")+" m/s"
                : "高度 --   速度 --";
            error.text=value.ErrorCode==VRShooting.Contracts.ErrorCode.None?string.Empty:"开场未完成："+value.ErrorCode+"，请返回或重新开始。";
            retry.gameObject.SetActive(value.Phase==DroneReconPhase.Error);
            retry.interactable=!busy;back.interactable=!busy;
            if(filter!=null)
            {filter.SetFloat("_Saturation",saturation);filter.SetColor("_MonitorTint",monitorTint);
                filter.SetFloat("_Noise",noise);filter.SetFloat("_Scanlines",scanlines);filter.SetFloat("_Vignette",vignette);}
        }
        public static string PhaseLabel(DroneReconPhase value)
        {
            switch(value)
            {
                case DroneReconPhase.PlayerTakeoff:return "无人机起飞 · 请从玩家视角观察";
                case DroneReconPhase.DroneRecon:return "堑壕侦察";
                case DroneReconPhase.DroneReturn:return "返回小队出生处";
                case DroneReconPhase.DroneLanding:return "无人机降落";
                case DroneReconPhase.RestoringPlayerView:return "恢复玩家视角";
                case DroneReconPhase.Ready:return "侦察完成";
                case DroneReconPhase.Error:return "侦察中断";
                case DroneReconPhase.Cancelled:return "已取消";
                default:return "准备无人机";
            }
        }
        void ReleaseBindings()
        {
            if(back!=null)back.onClick.RemoveListener(Back);
            if(retry!=null)retry.onClick.RemoveListener(Retry);
            if(feed!=null){feed.texture=null;if(feed.material==filter)feed.material=null;}
            if(filter!=null)Destroy(filter);
            filter=null;
        }
        void OnDestroy()
        {ReleaseBindings();BackRequested=RetryRequested=null;}
    }
}
