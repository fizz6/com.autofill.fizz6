using UnityEngine;

namespace Fizz6.Autofill.Test
{
    public class AutofillTest : MonoBehaviour
    {
        [SerializeField, Autofill]
        private Transform selfTransform;

        [SerializeField, Autofill(AutofillAttribute.Target.Parent)]
        private Transform[] parentTransforms;

        [SerializeField, Autofill(AutofillAttribute.Target.Children)]
        private Transform[] childrenTransforms;
    }
}