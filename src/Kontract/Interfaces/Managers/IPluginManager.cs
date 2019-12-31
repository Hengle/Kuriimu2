﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Models;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes methods to load files in the Kuriimu runtime.
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<IStateInfo> LoadFile(IFileSystem fileSystem, UPath path, IKuriimuProgress progress = null);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<IStateInfo> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IKuriimuProgress progress = null);

        Task SaveFile(IStateInfo stateInfo);
    }
}