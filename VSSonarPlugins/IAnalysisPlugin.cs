﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAnalysisPlugin.cs" company="Copyright © 2014 Tekla Corporation. Tekla is a Trimble Company">
//     Copyright (C) 2013 [Jorge Costa, Jorge.Costa@tekla.com]
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details. 
// You should have received a copy of the GNU Lesser General Public License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// --------------------------------------------------------------------------------------------------------------------
namespace VSSonarPlugins
{
    using System;
    using System.Collections.Generic;

    using VSSonarPlugins.Types;

    public interface IRoslynPlugin
    {
        void SetDiagnostics(List<DiagnosticAnalyzerType> diagnostics);
    }

    /// <summary>
    /// The Plugin interface.
    /// </summary>
    public interface IAnalysisPlugin : IPlugin
    {
        /// <summary>
        /// The get language.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        string GetLanguageKey(VsFileItem projectItem);

        /// <summary>
        /// The is supported.
        /// </summary>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        /// <param name="project">
        /// The resource.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsProjectSupported(ISonarConfiguration configuration, Resource project);

        /// <summary>
        /// The is supported.
        /// </summary>
        /// <param name="fileToAnalyse">
        /// The file to analyze.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsSupported(VsFileItem fileToAnalyse);

        /// <summary>
        /// The get resource key.
        /// </summary>
        /// <param name="projectItem">
        /// The project item.
        /// </param>
        /// <param name="bootStrapperOn"></param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        string GetResourceKey(VsFileItem projectItem, bool bootStrapperOn);

        /// <summary>
        /// The get local analysis extension.
        /// </summary>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        /// <returns>
        /// The <see cref="IFileAnalyser"/>.
        /// </returns>
        IFileAnalyser GetLocalAnalysisExtension(ISonarConfiguration configuration);

        /// <summary>The launch analysis on project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="configuration">The configuration.</param>
        void LaunchAnalysisOnProject(VsProjectItem project, ISonarConfiguration configuration);

        /// <summary>The launch analysis on solution.</summary>
        /// <param name="solution">The solution.</param>
        /// <param name="configuration">The configuration.</param>
        void LaunchAnalysisOnSolution(VsSolutionItem solution, ISonarConfiguration configuration);

        /// <summary>
        /// Gets the additional commands.
        /// </summary>
        /// <returns></returns>
        List<IPluginCommand> AdditionalCommands(Dictionary<string, Profile> profile);
    }
}


