using UnityEngine;

public class BorderWallShaderController : MonoBehaviour
{
    private Transform player;
    private static readonly int PlayerPosition = Shader.PropertyToID("_PlayerPosition");

    private void Update()
    {
        if (player == null) return;

        Shader.SetGlobalVector(
            PlayerPosition,
            player.position
        );
    }

    public void SetPlayer(Transform player) => this.player = player;
}
