using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WireConnector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    // �ΨӼȦs�쥻���Y�����z�]�w
    private Rigidbody plugRb;
    private bool wasKinematic;

    // �s���Ϊ����`
    private FixedJoint connectionJoint;

    void OnEnable()
    {
        socket.selectEntered.AddListener(OnPlugConnected);
        socket.selectExited.AddListener(OnPlugDisconnected);
    }

    void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnPlugConnected);
        socket.selectExited.RemoveListener(OnPlugDisconnected);
    }

    // �����Y���J��Ĳ�o
    public void OnPlugConnected(SelectEnterEventArgs args)
    {
        // 1. ���o���Y����
        GameObject plugObj = args.interactableObject.transform.gameObject;
        plugRb = plugObj.GetComponent<Rigidbody>();

        if (plugRb != null)
        {
            // 2. �j�������Y��� Socket �� Attach Point (���MSocket�|���A�����F���zí�w�A�T�O�@��)
            // �`�N�G�p�G�̿� Socket ���ت� Snapping�A�o�̥i�H�ٲ���m�]�w�A�M�`�󪫲z

            // 3. ����G�إߪ��z�s�� (Fixed Joint)
            // �o�|�����Y�u���z�W�v�H�b���y�W�A�Ӥ��O�ܦ��l����
            // �o�˪��z�O���ǻ��]�ϰʡ^�~�ब�ۼv�T
            connectionJoint = gameObject.AddComponent<FixedJoint>();
            connectionJoint.connectedBody = plugRb;

            // 4. �T�O���Y�O�����z�B�� (���n�ܦ� IsKinematic)
            // XR Socket �w�]�i��|�⪫���ܦ� Kinematic�A�ڭ̭n��^��
            wasKinematic = plugRb.isKinematic;
            plugRb.isKinematic = false;
        }
    }

    // �����Y�ޥX��Ĳ�o
    public void OnPlugDisconnected(SelectExitEventArgs args)
    {
        // 1. �������z�s��
        if (connectionJoint != null)
        {
            Destroy(connectionJoint);
        }

        // 2. ��_���Y�쥻�����z���A (���)
        if (plugRb != null)
        {
            // �q�`�ޤU�ӫ�|�Q���ۡAXR Grab Interactable �|����޲z���z�A
            // �ҥH�o�̳q�`���ݭn�S�O�]�^ Kinematic�A���D���S���ݨD�C
            plugRb = null;
        }
    }
}
