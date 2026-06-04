using UnityEngine;

public class SlaveObj : MonoBehaviour, IInteractable
{
    public void Interact(InteractionDetector player)
    {
        Debug.Log($"{gameObject.name}이 작동되었습니다.");
        SlaveAction();
    }

    // 인터랙션 동작
    void SlaveAction()
    {
        Debug.Log("작동됨 ㅅㄱ");
    }

    public void TargetHighlight(bool isLook)
    {
        // 플레이어가 작동 시킬 일이 생긴다면 하이라이팅 넣을것
    }
}
