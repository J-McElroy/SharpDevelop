﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using ICSharpCode.SharpDevelop.Project;
using NuGet;
using NUnit.Framework;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementServiceTests
	{
		PackageManagementService packageManagementService;
		OneRegisteredPackageSourceHelper packageSourcesHelper;
		FakePackageRepositoryFactory fakePackageRepositoryFactory;
		FakePackageManagerFactory fakePackageManagerFactory;
		FakePackageManagementProjectService fakeProjectService;
		TestableProject testProject;
		
		void CreatePackageSources()
		{
			packageSourcesHelper = new OneRegisteredPackageSourceHelper();
		}
		
		void CreatePackageManagementService()
		{
			CreatePackageSources();
			CreatePackageManagementService(packageSourcesHelper.Options);
		}
		
		void CreatePackageManagementService(PackageManagementOptions options)
		{
			testProject = ProjectHelper.CreateTestProject();
			fakePackageRepositoryFactory = new FakePackageRepositoryFactory();
			fakePackageManagerFactory = new FakePackageManagerFactory();
			fakeProjectService = new FakePackageManagementProjectService();
			fakeProjectService.CurrentProject = testProject;
			packageManagementService = 
				new PackageManagementService(options,
					fakePackageRepositoryFactory,
					fakePackageManagerFactory,
					fakeProjectService);		
		}
		
		[Test]
		public void InstallPackage_PackageObjectPassed_CallsPackageManagerInstallPackage()
		{
			CreatePackageManagementService();
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			bool expectedIgnoreDependencies = false;
			var expectedInstallPackageParameters = new FakePackageManager.InstallPackageParameters() {
				IgnoreDependenciesPassedToInstallPackage = expectedIgnoreDependencies,
				PackagePassedToInstallPackage = installPackageHelper.TestPackage
			};
			
			var actualInstallPackageParameters = fakePackageManagerFactory.FakePackageManager.ParametersPassedToInstallPackage;
			
			Assert.AreEqual(expectedInstallPackageParameters, actualInstallPackageParameters);
		}
		
		[Test]
		public void InstallPackage_PackageAndPackageRepositoryPassed_CreatesPackageManagerWithPackageRepository()
		{
			CreatePackageManagementService();
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			IPackageRepository expectedRepository = installPackageHelper.PackageRepository;
			IPackageRepository actualRepository = fakePackageManagerFactory.PackageRepositoryPassedToCreatePackageManager;
			
			Assert.AreEqual(expectedRepository, actualRepository);
		}
		
		[Test]
		public void InstallPackage_PackageAndPackageRepositoryPassed_CreatesPackageManagerWithCurrentlyActiveProject()
		{
			CreatePackageManagementService();
			IProject expectedProject = ProjectHelper.CreateTestProject();
			fakeProjectService.CurrentProject = expectedProject;
			
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			IProject actualProject = fakePackageManagerFactory.ProjectPassedToCreateRepository;
			
			Assert.AreEqual(expectedProject, actualProject);
		}
		
		[Test]
		public void InstallPackage_PackageAndPackageRepositoryPassed_RefreshesProjectBrowserWindow()
		{
			CreatePackageManagementService();
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			Assert.IsTrue(fakeProjectService.IsRefreshProjectBrowserCalled);
		}
		
		[Test]
		public void InstallPackage_PackageAndPackageRepositoryPassed_RefreshesProjectBrowserWindowAfterInstallingPackage()
		{
			CreatePackageManagementService();
			fakePackageManagerFactory.FakePackageManager.FakeProjectService = fakeProjectService;
			
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			bool result = fakePackageManagerFactory.FakePackageManager.IsRefreshProjectBrowserCalledWhenInstallPackageCalled;
			
			Assert.IsFalse(result);
		}
				
		[Test]
		public void ActivePackageRepository_OneRegisteredSource_RepositoryCreatedFromRegisteredSource()
		{
			CreatePackageManagementService();
			IPackageRepository activeRepository = packageManagementService.ActivePackageRepository;
			
			PackageSource actualPackageSource = fakePackageRepositoryFactory.FirstPackageSourcePassedToCreateRepository;
			
			Assert.AreEqual(packageSourcesHelper.PackageSource, actualPackageSource);
		}
		
		[Test]
		public void ActivePackageRepository_CalledTwice_RepositoryCreatedOnce()
		{
			CreatePackageManagementService();
			IPackageRepository activeRepository = packageManagementService.ActivePackageRepository;
			activeRepository = packageManagementService.ActivePackageRepository;
			
			int count = fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Count;
			
			Assert.AreEqual(1, count);
		}
		
		[Test]
		public void ActiveProjectManager_ProjectIsSelected_ReferencesSelectedProject()
		{
			CreatePackageManagementService();
			
			IProjectManager activeProjectManager = packageManagementService.ActiveProjectManager;
			IProject actualProject = fakePackageManagerFactory.ProjectPassedToCreateRepository;
			
			Assert.AreEqual(testProject, actualProject);
		}
		
		[Test]
		public void UninstallPackage_PackageObjectPassed_CallsPackageManagerUninstallPackage()
		{
			CreatePackageManagementService();
			
			FakePackage package = new FakePackage();
			package.Id = "Test";
			FakePackageRepository repository = new FakePackageRepository();
			packageManagementService.UninstallPackage(repository, package);
			
			var actualPackage = fakePackageManagerFactory.FakePackageManager.PackagePassedToUninstallPackage;
			
			Assert.AreEqual(package, actualPackage);
			Assert.AreEqual(repository, fakePackageManagerFactory.PackageRepositoryPassedToCreatePackageManager);
			Assert.AreEqual(testProject, fakePackageManagerFactory.ProjectPassedToCreateRepository);
		}
		
		[Test]
		public void UninstallPackage_PackageAndPackageRepositoryPassed_RefreshesProjectBrowserWindow()
		{
			CreatePackageManagementService();

			FakePackage package = new FakePackage();
			package.Id = "Test";
			FakePackageRepository repository = new FakePackageRepository();
			packageManagementService.UninstallPackage(repository, package);
			
			Assert.IsTrue(fakeProjectService.IsRefreshProjectBrowserCalled);
		}
		
		[Test]
		public void UninstallPackage_PackageAndPackageRepositoryPassed_RefreshesProjectBrowserWindowAfterUninstallingPackage()
		{
			CreatePackageManagementService();

			fakePackageManagerFactory.FakePackageManager.FakeProjectService = fakeProjectService;
			
			FakePackage package = new FakePackage();
			package.Id = "Test";
			FakePackageRepository repository = new FakePackageRepository();
			packageManagementService.UninstallPackage(repository, package);
			
			bool result = fakePackageManagerFactory.FakePackageManager.IsRefreshProjectBrowserCalledWhenUninstallPackageCalled;
			
			Assert.IsFalse(result);
		}
		
		[Test]
		public void PackageInstalled_PackageIsInstalled_EventFiresAfterPackageInstalled()
		{
			CreatePackageManagementService();
			
			IPackage package = null;
			packageManagementService.PackageInstalled += (sender, e) => {
				package = fakePackageManagerFactory.FakePackageManager.PackagePassedToInstallPackage;
			};
			InstallPackageHelper installPackageHelper = new InstallPackageHelper(packageManagementService);
			installPackageHelper.InstallTestPackage();
			
			Assert.AreEqual(installPackageHelper.TestPackage, package);
		}
		
		[Test]
		public void PackageUninstalled_PackageIsUninstalled_EventFiresAfterPackageUninstalled()
		{
			CreatePackageManagementService();
			
			IPackage package = null;
			packageManagementService.PackageUninstalled += (sender, e) => {
				package = fakePackageManagerFactory.FakePackageManager.PackagePassedToUninstallPackage;
			};
			FakePackage expectedPackage = new FakePackage();
			expectedPackage.Id = "Test";
			FakePackageRepository repository = new FakePackageRepository();
			packageManagementService.UninstallPackage(repository, expectedPackage);
			
			Assert.AreEqual(expectedPackage, package);
		}
		
		[Test]
		public void HasMultiplePackageSources_OnePackageSource_ReturnsFalse()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddOnePackageSource();
			
			bool result = packageManagementService.HasMultiplePackageSources;
			Assert.IsFalse(result);
		}
		
		[Test]
		public void HasMultiplePackageSources_TwoPackageSources_ReturnsTrue()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddTwoPackageSources();
			
			bool result = packageManagementService.HasMultiplePackageSources;
			Assert.IsTrue(result);
		}
		
		[Test]
		public void ActivePackageSource_TwoPackageSources_ByDefaultReturnsFirstPackageSource()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddTwoPackageSources();
			
			var expectedPackageSource = packageManagementService.Options.PackageSources[0];
			var packageSource = packageManagementService.ActivePackageSource;
			
			Assert.AreEqual(expectedPackageSource, packageSource);
		}
		
		[Test]
		public void ActivePackageSource_ChangedToSecondRegisteredPackageSources_ReturnsSecondPackageSource()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddTwoPackageSources();
			
			var expectedPackageSource = packageManagementService.Options.PackageSources[1];
			packageManagementService.ActivePackageSource = expectedPackageSource;
			var packageSource = packageManagementService.ActivePackageSource;
			
			Assert.AreEqual(expectedPackageSource, packageSource);
		}
		
		[Test]
		public void ActivePackageRepository_ActivePackageSourceChangedToSecondRegisteredPackageSource_CreatesRepositoryUsingSecondPackageSource()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddTwoPackageSources();
			
			var expectedPackageSource = packageManagementService.Options.PackageSources[1];
			packageManagementService.ActivePackageSource = expectedPackageSource;
			
			IPackageRepository repository = packageManagementService.ActivePackageRepository;
			var packageSource = fakePackageRepositoryFactory.FirstPackageSourcePassedToCreateRepository;
			
			Assert.AreEqual(expectedPackageSource, packageSource);
		}
		
		[Test]
		public void ActivePackageRepository_ActivePackageSourceChangedAfterActivePackageRepositoryCreated_CreatesNewRepositoryUsingActivePackageSource()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddTwoPackageSources();
			
			IPackageRepository initialRepository = packageManagementService.ActivePackageRepository;
			fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Clear();
			
			var expectedPackageSource = packageManagementService.Options.PackageSources[1];
			packageManagementService.ActivePackageSource = expectedPackageSource;
			
			IPackageRepository repository = packageManagementService.ActivePackageRepository;
			var packageSource = fakePackageRepositoryFactory.FirstPackageSourcePassedToCreateRepository;
			
			Assert.AreEqual(expectedPackageSource, packageSource);
		}
		
		[Test]
		public void ActivePackageRepository_ActivePackageSourceSetToSameValueAfterActivePackageRepositoryCreated_NewRepositoryNotCreated()
		{
			CreatePackageManagementService();
			packageSourcesHelper.AddOnePackageSource();
			
			IPackageRepository initialRepository = packageManagementService.ActivePackageRepository;
			fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Clear();
			
			var expectedPackageSource = packageManagementService.Options.PackageSources[0];
			packageManagementService.ActivePackageSource = expectedPackageSource;
			
			IPackageRepository repository = packageManagementService.ActivePackageRepository;
			
			Assert.AreEqual(0, fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Count);
		}
		
		[Test]
		public void ActivePackageSource_ChangedToNonNullPackageSource_SavedInOptions()
		{
			CreatePackageManagementService();
			
			packageManagementService.Options.ActivePackageSource = null;
			
			var packageSource = new PackageSource("http://source-url", "Test");
			packageManagementService.Options.PackageSources.Add(packageSource);
			packageManagementService.ActivePackageSource = packageSource;
			
			Assert.AreEqual(packageSource, packageManagementService.Options.ActivePackageSource);
		}
		
		[Test]
		public void ActivePackageSource_ActivePackageSourceNonNullInOptionsBeforeInstanceCreate_ActivePackageSourceReadFromOptions()
		{
			CreatePackageSources();
			var packageSource = new PackageSource("http://source-url", "Test");
			packageSourcesHelper.Options.PackageSources.Add(packageSource);
			packageSourcesHelper.Options.ActivePackageSource = packageSource;
			CreatePackageManagementService(packageSourcesHelper.Options);
			
			Assert.AreEqual(packageSource, packageManagementService.ActivePackageSource);
		}
		
		[Test]
		public void CreateAggregatePackageRepository_TwoRegisteredPackageRepositories_ReturnsPackagesFromBothRepositories()
		{
			CreatePackageSources();
			packageSourcesHelper.AddTwoPackageSources();
			CreatePackageManagementService(packageSourcesHelper.Options);
			
			var repository1Package = new FakePackage("One");
			var repository1 = new FakePackageRepository();
			repository1.FakePackages.Add(repository1Package);
			
			fakePackageRepositoryFactory.FakePackageRepositories.Add(packageSourcesHelper.RegisteredPackageSources[0], repository1);
			
			var repository2Package = new FakePackage("Two");
			var repository2 = new FakePackageRepository();
			repository2.FakePackages.Add(repository2Package);
			
			fakePackageRepositoryFactory.FakePackageRepositories.Add(packageSourcesHelper.RegisteredPackageSources[1], repository2);
			
			IPackageRepository repository = packageManagementService.CreateAggregatePackageRepository();
			IQueryable<IPackage> packages = repository.GetPackages().OrderBy(x => x.Id);
			
			var expectedPackages = new FakePackage[] {
				repository1Package,
				repository2Package
			};
			
			CollectionAssert.AreEqual(expectedPackages, packages);
		}
	}
}
