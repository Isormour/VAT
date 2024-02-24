using UnityEngine;

public interface IRenderStruct
{
   public int GetStructSize();
    public void SetParamsBufferData(ComputeBuffer buffer);
}