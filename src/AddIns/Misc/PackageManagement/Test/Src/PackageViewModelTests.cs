﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using NuGet;
using NUnit.Framework;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests
{
	[TestFixture]
	public class PackageViewModelTests
	{
		PackageViewModel viewModel;
		FakePackage package;
		FakePackageManagementService packageManagementService;
		FakePackageRepository packageSourceRepository;
		FakeLicenseAcceptanceService licenseAcceptanceService;
		
		void CreateViewModel()
		{
			package = new FakePackage();
			packageSourceRepository = new FakePackageRepository();
			packageManagementService = new FakePackageManagementService();
			licenseAcceptanceService = new FakeLicenseAcceptanceService();
			PackageViewModelFactory factory = new PackageViewModelFactory(packageManagementService, licenseAcceptanceService);
			packageManagementService.ActivePackageRepository = packageSourceRepository;
			viewModel = factory.CreatePackageViewModel(package);
		}
		
		FakePackage AddPackageDependencyThatDoesNotRequireLicenseAcceptance(string packageId)
		{
			return AddPackageDependency(package, packageId, false);
		}
		
		FakePackage AddPackageDependencyThatRequiresLicenseAcceptance(string packageId)
		{
			return AddPackageDependencyThatRequiresLicenseAcceptance(package, packageId);
		}
		
		FakePackage AddPackageDependencyThatRequiresLicenseAcceptance(FakePackage fakePackage, string packageId)
		{
			return AddPackageDependency(fakePackage, packageId, true);
		}

		FakePackage AddPackageDependency(FakePackage fakePackage, string packageId, bool requiresLicenseAcceptance)
		{
			fakePackage.AddDependency(packageId);
			
			var packageDependedUpon = new FakePackage(packageId);
			packageDependedUpon.RequireLicenseAcceptance = requiresLicenseAcceptance;
			
			packageSourceRepository.FakePackages.Add(packageDependedUpon);
			
			return packageDependedUpon;
		}
		
		[Test]
		public void AddPackageCommand_CommandExecuted_InstallsPackage()
		{
			CreateViewModel();
			viewModel.AddPackageCommand.Execute(null);
						
			Assert.AreEqual(package, packageManagementService.PackagePassedToInstallPackage);
			Assert.AreEqual(packageSourceRepository, packageManagementService.RepositoryPassedToInstallPackage);
		}
		
		[Test]
		public void AddPackage_PackageAddedSuccessfully_PropertyNotifyChangedFiredForIsAddedProperty()
		{
			CreateViewModel();
			string propertyChangedName = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedName = e.PropertyName;
			viewModel.AddPackage();
			
			Assert.AreEqual("IsAdded", propertyChangedName);
		}

		[Test]
		public void AddPackage_PackageAddedSuccessfully_PropertyNotifyChangedFiredAfterPackageInstalled()
		{
			CreateViewModel();
			IPackage packagePassedToInstallPackageWhenPropertyNameChanged = null;
			viewModel.PropertyChanged += (sender, e) => {
				packagePassedToInstallPackageWhenPropertyNameChanged = packageManagementService.PackagePassedToInstallPackage;
			};
			viewModel.AddPackage();
			
			Assert.AreEqual(package, packagePassedToInstallPackageWhenPropertyNameChanged);
		}

		[Test]
		public void HasLicenseUrl_PackageHasLicenseUrl_ReturnsTrue()
		{
			CreateViewModel();
			package.LicenseUrl = new Uri("http://sharpdevelop.com");
			
			Assert.IsTrue(viewModel.HasLicenseUrl);
		}
		
		[Test]
		public void HasLicenseUrl_PackageHasNoLicenseUrl_ReturnsFalse()
		{
			CreateViewModel();
			package.LicenseUrl = null;
			
			Assert.IsFalse(viewModel.HasLicenseUrl);
		}
		
		[Test]
		public void HasProjectUrl_PackageHasProjectUrl_ReturnsTrue()
		{
			CreateViewModel();
			package.ProjectUrl = new Uri("http://sharpdevelop.com");
			
			Assert.IsTrue(viewModel.HasProjectUrl);
		}
		
		[Test]
		public void HasProjectUrl_PackageHasNoProjectUrl_ReturnsFalse()
		{
			CreateViewModel();
			package.ProjectUrl = null;
			
			Assert.IsFalse(viewModel.HasProjectUrl);
		}
		
		[Test]
		public void HasReportAbuseUrl_PackageHasReportAbuseUrl_ReturnsTrue()
		{
			CreateViewModel();
			package.ReportAbuseUrl = new Uri("http://sharpdevelop.com");
			
			Assert.IsTrue(viewModel.HasReportAbuseUrl);
		}
		
		[Test]
		public void HasReportAbuseUrl_PackageHasNoReportAbuseUrl_ReturnsFalse()
		{
			CreateViewModel();
			package.ReportAbuseUrl = null;
			
			Assert.IsFalse(viewModel.HasReportAbuseUrl);
		}
		
		[Test]
		public void IsAdded_ProjectHasPackageAdded_ReturnsTrue()
		{
			CreateViewModel();
			packageManagementService.FakeActiveProjectManager.IsInstalledReturnValue = true;
			
			Assert.IsTrue(viewModel.IsAdded);
		}
		
		[Test]
		public void IsAdded_ProjectDoesNotHavePackageInstalled_ReturnsFalse()
		{
			CreateViewModel();
			packageManagementService.FakeActiveProjectManager.IsInstalledReturnValue = false;
			
			Assert.IsFalse(viewModel.IsAdded);
		}
		
		[Test]
		public void RemovePackageCommand_CommandExecuted_UninstallsPackage()
		{
			CreateViewModel();
			viewModel.RemovePackageCommand.Execute(null);
						
			Assert.AreEqual(package, packageManagementService.PackagePassedToUninstallPackage);
			Assert.AreEqual(packageSourceRepository, packageManagementService.RepositoryPassedToUninstallPackage);
		}
		
		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_PropertyNotifyChangedFiredForIsAddedProperty()
		{
			CreateViewModel();
			string propertyChangedName = null;
			viewModel.PropertyChanged += (sender, e) => propertyChangedName = e.PropertyName;
			viewModel.RemovePackage();
			
			Assert.AreEqual("IsAdded", propertyChangedName);
		}
		
		[Test]
		public void RemovePackage_PackageRemovedSuccessfully_PropertyNotifyChangedFiredAfterPackageUninstalled()
		{
			CreateViewModel();
			IPackage packagePassedToUninstallPackageWhenPropertyNameChanged = null;
			viewModel.PropertyChanged += (sender, e) => {
				packagePassedToUninstallPackageWhenPropertyNameChanged = packageManagementService.PackagePassedToUninstallPackage;
			};
			viewModel.RemovePackage();
			
			Assert.AreEqual(package, packagePassedToUninstallPackageWhenPropertyNameChanged);
		}
		
		[Test]
		public void HasDependencies_PackageHasNoDependencies_ReturnsFalse()
		{
			CreateViewModel();
			package.DependenciesList.Clear();
			
			Assert.IsFalse(viewModel.HasDependencies);
		}
		
		[Test]
		public void HasDependencies_PackageHasOneDependency_ReturnsTrue()
		{
			CreateViewModel();
			package.DependenciesList.Add(new PackageDependency("Test"));
			
			Assert.IsTrue(viewModel.HasDependencies);
		}
		
		[Test]
		public void HasNoDependencies_PackageHasNoDependencies_ReturnsTrue()
		{
			CreateViewModel();
			package.DependenciesList.Clear();
			
			Assert.IsTrue(viewModel.HasNoDependencies);
		}
		
		[Test]
		public void HasNoDependencies_PackageHasOneDependency_ReturnsFalse()
		{
			CreateViewModel();
			package.DependenciesList.Add(new PackageDependency("Test"));
			
			Assert.IsFalse(viewModel.HasNoDependencies);
		}
		
		[Test]
		public void HasDownloadCount_DownloadCountIsZero_ReturnsTrue()
		{
			CreateViewModel();
			package.DownloadCount = 0;
			
			Assert.IsTrue(viewModel.HasDownloadCount);
		}
		
		[Test]
		public void HasDownloadCount_DownloadCountIsMinusOne_ReturnsFalse()
		{
			CreateViewModel();
			package.DownloadCount = -1;
			
			Assert.IsFalse(viewModel.HasDownloadCount);
		}
				
		[Test]
		public void AddPackage_PackageRequiresLicenseAgreementAcceptance_UserAskedToAcceptLicenseAgreementForPackageBeforeInstalling()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = true;
			licenseAcceptanceService.AcceptLicensesReturnValue = true;
			viewModel.AddPackage();
			
			var expectedPackages = new FakePackage[] {
				package
			};
			
			var actualPackages = licenseAcceptanceService.PackagesPassedToAcceptLicenses;
			
			CollectionAssert.AreEqual(expectedPackages, actualPackages);
		}
		
		[Test]
		public void AddPackage_PackageDoesNotRequireLicenseAgreementAcceptance_UserNotAskedToAcceptLicenseAgreementBeforeInstalling()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = false;
			viewModel.AddPackage();
			
			Assert.IsFalse(licenseAcceptanceService.IsAcceptLicensesCalled);
		}
		
		[Test]
		public void AddPackage_PackageRequiresLicenseAgreementAcceptanceAndUserDeclinesAgreement_PackageIsNotInstalled()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = true;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;
			viewModel.AddPackage();
			
			Assert.IsFalse(packageManagementService.IsInstallPackageCalled);
		}
		
		[Test]
		public void AddPackage_PackageRequiresLicenseAgreementAcceptanceAndUserDeclinesAgreement_PropertyChangedEventNotFired()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = true;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;
			bool propertyChangedEventFired = false;
			viewModel.PropertyChanged += (sender, e) => propertyChangedEventFired = true;
			viewModel.AddPackage();
			
			Assert.IsFalse(propertyChangedEventFired);
		}
		
		[Test]
		public void AddPackage_PackageHasOneDependencyThatRequiresLicenseAgreementAcceptance_UserAskedToAcceptLicenseForPackageDependency()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = false;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;
			FakePackage packageDependedUpon = 
				AddPackageDependencyThatRequiresLicenseAcceptance("PackageDependencyId");
			
			viewModel.AddPackage();
			
			var expectedPackages = new FakePackage[] {
				packageDependedUpon
			};
			
			var actualPackages = licenseAcceptanceService.PackagesPassedToAcceptLicenses;
			
			CollectionAssert.AreEqual(expectedPackages, actualPackages);
		}
		
		[Test]
		public void AddPackage_PackageAndPackageDependencyRequiresLicenseAgreementAcceptance_UserAskedToAcceptLicenseForPackageAndPackageDependency()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = true;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;
			FakePackage packageDependedUpon = 
				AddPackageDependencyThatRequiresLicenseAcceptance("PackageDependencyId");
			
			viewModel.AddPackage();
			
			var expectedPackages = new FakePackage[] {
				package,
				packageDependedUpon
			};
			
			var actualPackages = licenseAcceptanceService.PackagesPassedToAcceptLicenses;
			
			CollectionAssert.AreEqual(expectedPackages, actualPackages);
		}
		
		[Test]
		public void AddPackage_PackageHasOneDependencyThatDoesNotRequireLicenseAgreementAcceptance_UserNotAskedToAcceptLicenseForPackageDependency()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = false;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;			
			AddPackageDependencyThatDoesNotRequireLicenseAcceptance("PackageDependencyId");
			
			viewModel.AddPackage();
			
			Assert.IsFalse(licenseAcceptanceService.IsAcceptLicensesCalled);
		}
		
		[Test]
		public void AddPackage_PackageDependencyHasDependencyThatRequiresLicenseAcceptance_UserAskedToAcceptLicenseForPackageDependencyChildPackage()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = false;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;
			FakePackage packageDependedUpon = 
				AddPackageDependencyThatDoesNotRequireLicenseAcceptance("ParentPackageIdForDependency");
			
			FakePackage childPackageDependedUpon =
				AddPackageDependencyThatRequiresLicenseAcceptance(
					packageDependedUpon,
					"ChildPackageIdForDependency");
			
			viewModel.AddPackage();
			
			var expectedPackages = new FakePackage[] {
				childPackageDependedUpon
			};
			
			var actualPackages = licenseAcceptanceService.PackagesPassedToAcceptLicenses;
			
			CollectionAssert.AreEqual(expectedPackages, actualPackages);			
		}
		
		[Test]
		public void AddPackage_PackageHasOneDependencyThatRequiresLicenseAgreementAcceptanceButIsAlreadyInstalledLocally_UserIsNotAskedToAcceptLicenseForPackageDependency()
		{
			CreateViewModel();
			package.RequireLicenseAcceptance = false;
			licenseAcceptanceService.AcceptLicensesReturnValue = false;			
			var packageDependedUpon = AddPackageDependencyThatRequiresLicenseAcceptance("PackageDependencyId");
			packageManagementService.AddPackageToProjectLocalRepository(packageDependedUpon);
			packageManagementService.FakeActiveProjectManager.IsInstalledReturnValue = true;
			
			viewModel.AddPackage();
			
			Assert.IsFalse(licenseAcceptanceService.IsAcceptLicensesCalled);
		}
	}
}
