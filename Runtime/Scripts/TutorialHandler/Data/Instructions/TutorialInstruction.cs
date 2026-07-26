using System.Collections.Generic;

namespace tglGames.tutorial_manager.tgl_tutorial_handler.data
{
    [System.Serializable]
    public class TutorialInstruction : BlockInstruction
    {
        public List<TutorialDisplayStruct> targets;
    }
}
