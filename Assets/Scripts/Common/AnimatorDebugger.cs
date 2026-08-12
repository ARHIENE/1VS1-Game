using UnityEngine;
using Photon.Pun;

public class AnimatorDebugger : MonoBehaviour
{
    private Animator anim;
    private PhotonView pv;

    private void Start()
    {
        anim = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) { Destroy(this); return; }
    }

    private void Update()
    {
        if (Time.frameCount % 120 == 0) // Log once every 2 seconds
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            var clips = anim.GetCurrentAnimatorClipInfo(0);
            
            string clipInfo = "";
            foreach(var c in clips) {
                clipInfo += $"{c.clip.name} (Loop:{c.clip.isLooping}), ";
            }

            Debug.Log($"[AnimDebug] State: {GetCurrentStateName(info)}, SpeedVal: {anim.GetFloat("Speed")}, Grounded: {anim.GetBool("IsGrounded")}, Crouching: {anim.GetBool("IsCrouching")}, Clips: {clipInfo}");
        }
    }

    private string GetCurrentStateName(AnimatorStateInfo info)
    {
        if (info.IsName("Movement")) return "Movement";
        if (info.IsName("Crouch")) return "Crouch";
        if (info.IsName("Jump")) return "Jump";
        return "Other";
    }
}
