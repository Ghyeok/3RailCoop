using Steamworks;
using UnityEngine;

public static class SteamAvatarUtility
{
    public static Texture2D GetSteamAvatar(CSteamID steamId)
    {
        // 1. 대형 아바타 핸들 요청
        int imageId = SteamFriends.GetLargeFriendAvatar(steamId);

        if (imageId <= 0) return null;

        // 2. 가로, 세로 크기 확인
        uint width, height;
        if(!SteamUtils.GetImageSize(imageId, out width, out height)) return null;

        // 3. RGBA 바이트 배열 준비 (가로 x 세로 x 4채널)
        byte[] rawImage = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(imageId, rawImage, (int)(width * height * 4))) return null;

        // 4. 상하 반전(Flip) 처리
        byte[] flippedImage = new byte[rawImage.Length];
        int bytesPerRow = (int)width * 4;
        for(int y = 0; y < height; y++)
        {
            System.Array.Copy(
                rawImage,
                y * bytesPerRow,
                flippedImage,
                (height - 1 - y) * bytesPerRow,
                bytesPerRow
                );
        }

        // 5. 유니티 Texture2D 생성 및 데이터 적용
        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(flippedImage);
        texture.Apply();

        return texture;
    }
}