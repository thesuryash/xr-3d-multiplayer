namespace XRMultiplayer.ContentPipeline
{
    public static class ModelModerationGate
    {
        public static bool IsSpawnableInLiveRoom(ProcessedModelMetadata metadata, bool hostApproved)
        {
            if (metadata == null)
                return false;

            if (metadata.CurrentModerationState == ModerationState.Rejected)
                return false;

            if (metadata.CurrentModerationState == ModerationState.Approved)
                return true;

            return hostApproved;
        }

        public static ModerationState ResolveInitialState(ModelIngestionPolicy policy, string sourceGuid)
        {
            if (policy == null)
                return ModerationState.PendingHostApproval;

            if (policy.Moderation == ModelIngestionPolicy.ModerationMode.Allowlist)
                return policy.IsGuidAllowlisted(sourceGuid) ? ModerationState.Approved : ModerationState.Rejected;

            return ModerationState.PendingHostApproval;
        }
    }
}
