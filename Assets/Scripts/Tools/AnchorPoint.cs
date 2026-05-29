using UnityEngine;

public class AnchorPoint : MonoBehaviour
{
    GameObject _midder;
    GameObject _bottom;
    public void SetUp(Collider collider,GameObject parents)
    {
        _midder = transform.Find("Middle").gameObject;
        _bottom = transform.Find("Bottom").gameObject;


        //設置所有錨點初始位置
        transform.position = collider.bounds.center;
        _midder.transform.localPosition = Vector3.zero;
        _bottom.transform.localPosition = new Vector3(0, -((collider.bounds.size.y/2)/ parents.transform.localScale.y), 0);
    }
    public GameObject bottom { get { return _bottom; } }
    public GameObject midder { get { return _midder; } }
}
