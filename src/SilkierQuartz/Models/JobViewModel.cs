﻿using Microsoft.AspNetCore.Mvc.Rendering;
using SilkierQuartz.Helpers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SilkierQuartz.Models
{
    public class JobViewModel : IHasValidation
    {
        public JobPropertiesViewModel Job { get; set; }
        public JobDataMapModel DataMap { get; set; }

        public void Validate(ICollection<ValidationError> errors) => ModelValidator.ValidateObject(this, errors);
    }

    public class JobPropertiesViewModel : IHasValidation
    {
        public bool IsNew { get; set; }

        public bool IsCopy { get; set; }

        [Required]
        public string JobName { get; set; }

        [Required]
        public string Group { get; set; }

        public string OldJobName { get; set; }
        public string OldGroup { get; set; }

        public IEnumerable<SelectListItem> GroupList { get; set; }

        [Required]
        public string Type { get; set; }

        public IEnumerable<SelectListItem> TypeList { get; set; }

        public string Description { get; set; }

        public bool Recovery { get; set; }

        public void Validate(ICollection<ValidationError> errors) => ModelValidator.ValidateObject(this, errors, nameof(JobViewModel.Job));
    }

}
