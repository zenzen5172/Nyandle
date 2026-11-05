using UnityEngine;

// 火の矢などの「火」が当たったときに反応するもの（松明・導火線・綱・コウモリなど）
public interface IIgnitable
{
    // source: 火を当てたオブジェクト（火の矢など）
    void Ignite(GameObject source);
}
