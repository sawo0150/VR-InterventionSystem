using UnityEngine;

namespace Project
{
    public class MyMemo : MonoBehaviour
    {
        [TextArea(5, 20)]
        public string description;
    }
}
