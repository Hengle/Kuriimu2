﻿using System.Collections.Generic;
using System.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_level5.Archives
{
    class Arc0State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Arc0 _arc0;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged { get; set; }

        public Arc0State()
        {
            _arc0 = new Arc0();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _arc0.Load(fileStream);
        }

        public void Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _arc0.Save(output, Files);
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}