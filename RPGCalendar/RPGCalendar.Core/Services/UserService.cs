﻿namespace RPGCalendar.Core.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Dto;
    using Repositories;
    using User = Data.User;

    public interface IUserService
    {
        public Task<Dto.User?> RegisterUser(UserInput userInput);
        public Task<Dto.User?> LoginUser(string authId);
        public void LogoutUser();
        public Task<User?> GetUserById(int userId);
        public Task<List<Dto.User>> GetPlayersList();
        public Task<Dto.User?> GetPlayer();
        public Task<Dto.User?> GetUserByIdAsync(int userId);
    }
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly ISessionService _sessionService;
        private readonly IUserRepository _userRepository;

        public UserService(IMapper mapper, ISessionService sessionService, IUserRepository userRepository)
        {
            _mapper = mapper;
            _sessionService = sessionService;
            _userRepository = userRepository;
        }

        public async Task<Dto.User?> RegisterUser(UserInput userInput)
        {
            User user = _mapper.Map<UserInput, User>(userInput);
            await _userRepository.InsertAsync(user);
            _sessionService.SetCurrentUserId(user.Id);
            return _mapper.Map<User, Dto.User>(user);
        }

        public async Task<Dto.User?> LoginUser(string authId)
        {
            var user = await _userRepository.GetUserByAuthId(authId);
            if (user is null)
                return null;
            _sessionService.SetCurrentUserId(user.Id);
            return _mapper.Map<User, Dto.User>(user);
        }

        public void LogoutUser()
        {
            _sessionService.ClearSessionUser();
            _sessionService.ClearSessionGame();
        }

        public async Task<User?> GetUserById(int userId)
            => await _userRepository.GetUserById(userId);

        public async Task<List<Dto.User>> GetPlayersList()
        {
            var gameId = _sessionService.GetCurrentGameId();
            var players = (await _userRepository.FetchAllAsync())
                .Where(a => a.GameUsers.Any(e => e.GameId == gameId))
                .Select(MapUser);
            return players.ToList();
        }
        
        public async Task<Dto.User?> GetPlayer()
        {
            var player = await _userRepository.FetchByIdAsync(_sessionService.GetCurrentUserId());
            if (player is null)
                return null;
            return MapUser(player);
        }

        public async Task<Dto.User?> GetUserByIdAsync(int id)
        {
            var gameId = _sessionService.GetCurrentGameId();
            var user = (await _userRepository.FetchAllAsync())
                .First(a => a.GameUsers.Any(e => e.GameId == gameId && e.User.Id == id));
            if (user is null)
                return null;
            return MapUser(user);
        }

        public Dto.User MapUser(User user)
        {
            var gameUser = user.GameUsers.First(e => e.GameId == _sessionService.GetCurrentGameId());
            var dtoUser = new Dto.User
            {
                Username = user.Username,
                Email = user.Email,
                Class = gameUser.PlayerClass,
                Bio = gameUser.PlayerBio,
                Id = user.Id
            };
            return dtoUser;
        }
    }
}
