using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class AiTransportPlayModeTests
{
    sealed class PacketTransport : IAiTransport
    {
        public IEnumerator Send(AiRequest request, Action<string> onDelta, Action<AiResult> onDone)
        {
            onDelta?.Invoke("first ");
            yield return null;
            onDelta?.Invoke("packet");
            onDone?.Invoke(new AiResult { Success = true, Text = "first packet", Model = "fake" });
        }
    }

    [UnityTest]
    public IEnumerator GeminiClient_ForwardsIncrementalPacketsAndTypedCompletion()
    {
        IAiTransport previous = GeminiClient.Transport;
        try
        {
            GeminiClient.Transport = new PacketTransport();
            string visible = "";
            AiResult completed = null;
            yield return GeminiClient.Stream(
                new AiRequest { Feature = AiFeature.Dialogue, Prompt = "test" },
                delta => visible += delta,
                result => completed = result);

            Assert.AreEqual("first packet", visible);
            Assert.IsNotNull(completed);
            Assert.IsTrue(completed.Success);
            Assert.AreEqual("fake", completed.Model);
        }
        finally
        {
            GeminiClient.Transport = previous;
        }
    }
}
