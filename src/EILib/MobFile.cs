using System;
using System.Collections.Generic;
using System.IO;
using static EILib.MobFileSection;

namespace EILib
{
    public class MobFile
    {
        public MobFileSection CurrentSection;
        private MobFileSection _mainSection;

        public MobFile()
        {
            CurrentSection = null;
            _mainSection = null;
        }

        public MobFile(string filename)
            : this()
        {
            OpenMobFile(filename);
        }

        public void OpenMobFile(string filename)
        {
            _mainSection = new MobFileSection(File.ReadAllBytes(filename), null);
            CurrentSection = _mainSection;
        }

        public void SaveMobFile(string filename)
        {
            if (_mainSection == null)
                return;

            File.WriteAllBytes(filename, _mainSection.GetData());
        }

        public bool OpenSectionById(ESectionId id)
        {
            foreach (var i in CurrentSection.Items)
            {
                if (i.Id == id)
                {
                    CurrentSection = i;
                    return true;
                }
            }

            return false;
        }

        public void OpenScriptSection()
        {
            CurrentSection = _mainSection;
            if (!OpenSectionById(ESectionId.OBJECTDBFILE) ||
                (!OpenSectionById(ESectionId.SS_TEXT) && !OpenSectionById(ESectionId.SS_TEXT_OLD)))
            {
                throw new InvalidOperationException();
            }
        }

        public void OpenObjectsSection()
        {
            CurrentSection = _mainSection;
            if (!OpenSectionById(ESectionId.OBJECTDBFILE) ||
                !OpenSectionById(ESectionId.OBJECTSECTION))
            {
                throw new InvalidOperationException();
            }
        }

        public void LeaveSection()
        {
            if (CurrentSection.Owner != null)
                CurrentSection = CurrentSection.Owner;
        }
    }
}
