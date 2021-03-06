﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TweetBookAPI.Constracts.V1;
using TweetBookAPI.Constracts.V1.Requests;
using TweetBookAPI.Constracts.V1.Responses;
using TweetBookAPI.Domain;
using TweetBookAPI.Extensions;
using TweetBookAPI.Services;

namespace TweetBookAPI.Controllers.V1
{
    //[Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Produces(contentType:"application/json")]
    public class TagsController : ControllerBase
    {
        private IPostService _postService;
        private readonly IMapper _mapper;

        public TagsController(IPostService postService,IMapper mapper)
        {
            _postService = postService;
            _mapper = mapper;
        }

        /// <summary>
        /// Returns all the tags in the system
        /// </summary>
        /// <response code="200">Returns all the tags in the system</response>
        /// <returns></returns>
        [HttpGet(ApiRoutes.Tags.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _postService.GetAllTagsAsync();
            return Ok(_mapper.Map<List<TagResponse>>(tags));
        }

        [HttpGet(ApiRoutes.Tags.Get)]
        public async Task<IActionResult> Get([FromRoute]string tagName)
        {
            var tag = await _postService.GetTagByNameAsync(tagName);

            if (tag == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TagResponse>(tag));
        }


        /// <summary>
        /// Creates a tag in the system
        /// </summary>
        /// <remarks>
        ///     Sample **request**:
        /// 
        ///     POST /api/v1/tags
        ///     {
        ///         "tagname":"sample name"
        ///     }
        /// </remarks>
        /// <response code="201">Creates a tag in the system</response>
        /// <response code="400">Unable to create a tag due to validation error</response>
        /// <returns></returns>
        [HttpPost(ApiRoutes.Tags.Create)]
        [ProducesResponseType(typeof(TagResponse),statusCode:201)]
        [ProducesResponseType(typeof(ErrorResponse), statusCode: 400)]
        public async Task<IActionResult> Create([FromBody] CreateTagRequest request)
        {
            var newTag = new Tag
            {
                Name = request.TagName,
                CreatorId = HttpContext.GetUserId(),
                CreatedOn = DateTime.UtcNow
            };

            var created = await _postService.CreateTagAsync(newTag);
            if (!created)
            {
                //return BadRequest(new { error = "Unable to create tag" });
                return BadRequest(new ErrorResponse
                {
                    Errors = new List<ErrorModel>
                    {
                        new ErrorModel{Message="Unable to create tag"}
                    }
                });
            }

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
            var locationUri = baseUrl + "/" + ApiRoutes.Tags.Get.Replace("{tagName}", newTag.Name);
            return Created(locationUri, _mapper.Map<TagResponse>(newTag));
        }


        [HttpDelete(ApiRoutes.Tags.Delete)]
        [Authorize(policy: "MustWorkForABCCompany")]//only emails with domain abccompany.com can access this
        public async Task<IActionResult> Delete([FromRoute] string tagName)
        {
            var deleted = await _postService.DeleteTagAsync(tagName);

            if (deleted)
                return NoContent();

            return NotFound();
        }
    }
}